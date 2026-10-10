"""生成数学桌球九张源配置表；导出仍使用仓库 Luban 脚本。"""
from pathlib import Path
import json
from openpyxl import Workbook, load_workbook
from openpyxl.styles import Font, PatternFill, Alignment
from xml.etree.ElementTree import Element, SubElement, indent, tostring
from Campaign import create_campaign

root = Path(__file__).resolve().parents[2] / 'Configs/GameConfig'
schema = Element('module', name='')

def enum(name, names):
    e = SubElement(schema, 'enum', name=name)
    for i, n in enumerate(names): SubElement(e, 'var', name=n, value=str(i))

def bean(name, fields=(), parent=None):
    b = SubElement(schema, 'bean', name=name, **({'parent': parent} if parent else {}))
    for n, t in fields: SubElement(b, 'var', name=n, type=t)

enum('BallKind', ['White', 'Black'])
enum('PocketSlot', ['TopLeft', 'TopMiddle', 'TopRight', 'BottomLeft', 'BottomMiddle', 'BottomRight'])
enum('PocketState', ['Locked', 'Unlocked', 'Occupied', 'Disabled'])
enum('ConditionKind', ['Inscribed', 'Circumscribed'])
enum('TriggerMode', ['Contact', 'Geometry'])
enum('PropKind', ['Flip', 'Reverse', 'Freeze', 'Restore', 'FastChange', 'Reveal', 'Preview', 'AddShot', 'Key'])
enum('UseScope', ['Shot', 'Level'])
enum('DecelerationModel', ['ShotDistance'])
enum('TerrainKind', ['JumpPad', 'Tunnel', 'Pit', 'ReverseBelt', 'Teleporter'])
bean('ShotParams', [(n, 'float') for n in ['chargeDuration', 'speedMin', 'speedMax', 'radiusMin', 'radiusMax', 'distanceMin', 'distanceMax']])
bean('SizeChangeParams', [('growRateMin','float'), ('growRateMax','float'), ('shrinkRate','float')])
bean('Shape')
bean('Triangle', [('a','vector2'), ('b','vector2'), ('c','vector2')], 'Shape')
bean('EffectBinding')
bean('UnlockPocket', [('pocketPlacementId','int#ref=TbLevelPocket')], 'EffectBinding')
bean('TriggerProp', [('propPlacementId','int#ref=TbLevelProp')], 'EffectBinding')
bean('PropEffect', [('durationShots','int'), ('rateMultiplier','float'), ('targetPocketId','int')])
for effect_name in ['FlipEffect', 'ReverseEffect', 'FreezeEffect', 'RestoreEffect',
                    'FastChangeEffect', 'RevealEffect', 'PreviewEffect',
                    'AddShotEffect', 'KeyEffect']:
    bean(effect_name, (), 'PropEffect')
bean('PocketLayout', [('slot','PocketSlot'), ('offset','vector2'), ('mouthWidth','float'), ('captureRadius','float')])
bean('PocketStyle', [('state','PocketState'), ('color','vector4')])
bean('GeometryStyle', [('figureColor','vector4'), ('circleColor','vector4'), ('lineWidth','float'),
                       ('previewColor','vector4'), ('previewLineWidth','float'),
                       ('previewFailureThreshold','int'), ('previewFailureInterval','int'),
                       ('previewDuration','float')])
bean('PredictionStyle', [('color','vector4'), ('lineWidth','float'), ('dashLength','float'), ('gapLength','float'), ('dotDiameter','float')])
bean('TerrainStyle', [('terrainKind','TerrainKind'), ('prefabLocation','string'), ('defaultDuration','float'),
                      ('defaultSpeedMultiplier','float'), ('inheritVelocity','bool'), ('inheritRotation','bool')])

tables = load_workbook(root / 'Datas/__tables__.xlsx')
ws = tables.active
# 只更新本工具维护的九张表注册，保留其他配置表。
owned_tables = {'Tb' + name for name in (
    'Level', 'Board', 'Ball', 'Prop', 'Physics',
    'LevelBall', 'LevelPocket', 'LevelProp', 'LevelGeometry', 'Terrain', 'LevelTerrain')}
for index in range(ws.max_row, 3, -1):
    if ws.cell(index, 2).value in owned_tables:
        ws.delete_rows(index)

def table(name, fields, rows):
    ws.append([None, 'Tb'+name, name, True, name+'.xlsx', 'id', 'map', 'c', name, None, None])
    book = Workbook(); sheet = book.active; sheet.title = name
    sheet.append(['##var']+[n+('#format=json' if t not in ['int','float','bool','string'] and not t.startswith('int#') and t not in ['BallKind','PocketSlot','PocketState','ConditionKind','TriggerMode','PropKind','UseScope','DecelerationModel','TerrainKind'] else '') for n,t in fields])
    sheet.append(['##type']+[t for n,t in fields])
    sheet.append(['##comment']+[n for n,t in fields])
    sheet.append(['##group']+['c']*len(fields))
    for row in rows:
        sheet.append([None]+[json.dumps(v,ensure_ascii=False,separators=(',',':')) if isinstance(v,(dict,list)) else v for v in row])
    sheet.freeze_panes = 'C5'
    for row in sheet.iter_rows(min_row=1,max_row=4):
        for cell in row:
            cell.font=Font(color='FFFFFF'); cell.fill=PatternFill('solid',fgColor='244C46')
            cell.alignment=Alignment(wrap_text=True)
    for i in range(1,len(fields)+2): sheet.column_dimensions[sheet.cell(1,i).column_letter].width=24
    book.save(root / ('Datas/'+name+'.xlsx'))

ref=lambda table:'int#ref=Tb'+table
vec=lambda x,y:dict(x=x,y=y)
color=lambda r,g,b,a=1:dict(x=r,y=g,z=b,w=a)
campaign = create_campaign()
table('Level',[('id','int'),('name','string'),('order','int'),('boardConfigId',ref('Board')),('physicsConfigId',ref('Physics')),('maxShots','int'),('showGeometryHintsByDefault','bool'),('difficulty','string'),('objective','string')],[[i,name,i,1,1,shots,True,difficulty,objective] for i,name,difficulty,objective,shots in campaign['levels']])
layouts=[dict(slot=s,offset=vec(0,0),mouthWidth=1.5,captureRadius=.58) for s in ['TopLeft','TopMiddle','TopRight','BottomLeft','BottomMiddle','BottomRight']]
styles=[dict(state=s,color=c) for s,c in [('Locked',color(.85,.45,.13)),('Unlocked',color(.05,.08,.07)),('Occupied',color(.3,.8,.55)),('Disabled',color(.48,.48,.5))]]
table('Board',[('id','int'),('prefabLocation','string'),('min','vector2'),('max','vector2'),('pocketLayouts','list,PocketLayout'),('pocketStyles','list,PocketStyle'),('geometryStyle','GeometryStyle'),('predictionStyle','PredictionStyle')],[[1,'BilliardsTable',vec(-8,-4),vec(8,4),layouts,styles,dict(figureColor=color(1,.79,.33),circleColor=color(.43,.93,.85,.7),lineWidth=.035,previewColor=color(1,.33,.16,.95),previewLineWidth=.08,previewFailureThreshold=5,previewFailureInterval=2,previewDuration=1.2),dict(color=color(.76,.96,.89,.7),lineWidth=.025,dashLength=.18,gapLength=.12,dotDiameter=.12)]])
shot=dict(chargeDuration=3.0,speedMin=4,speedMax=8,radiusMin=.25,radiusMax=1,distanceMin=4,distanceMax=18)
size_change = dict(growRateMin=.06, growRateMax=.18, shrinkRate=.12)
# 二十关统一使用白球 ID 1；暂时保留未引用的 ID 3，避免在本轮混入配置删除。
table('Ball',[('id','int'),('ballKind','BallKind'),('prefabLocation','string'),('mass','float'),('radiusMin','float'),('radiusMax','float'),('shotParams','ShotParams?'),('sizeChangeParams','SizeChangeParams?')],[[1,'White','BilliardsWhiteBall',1,.15,1.6,shot,size_change],[2,'Black','BilliardsBlackBall',1,.28,.28,None,None],[3,'White','BilliardsWhiteBall',1,.15,1.6,shot,size_change]])
prop_rows = [
    [1, 'Flip', 'BilliardsFlipProp', .468, .234,
     {'$type': 'FlipEffect', 'durationShots': 1, 'rateMultiplier': 1, 'targetPocketId': 0}, 'Shot', 1],
    [2, 'Flip', 'BilliardsFlipProp', .468, .234,
     {'$type': 'FlipEffect', 'durationShots': 1, 'rateMultiplier': 1, 'targetPocketId': 0}, 'Level', 1],
    [3, 'Reverse', 'BilliardsReverseProp', .468, .234,
     {'$type': 'ReverseEffect', 'durationShots': 0, 'rateMultiplier': 1, 'targetPocketId': 0}, 'Level', 1],
    [4, 'Freeze', 'BilliardsFreezeProp', .468, .234,
     {'$type': 'FreezeEffect', 'durationShots': 1, 'rateMultiplier': 0, 'targetPocketId': 0}, 'Shot', 1],
    [5, 'Restore', 'BilliardsRestoreProp', .468, .234,
     {'$type': 'RestoreEffect', 'durationShots': 0, 'rateMultiplier': 1, 'targetPocketId': 0}, 'Shot', 1],
    [6, 'FastChange', 'BilliardsFastChangeProp', .468, .234,
     {'$type': 'FastChangeEffect', 'durationShots': 1, 'rateMultiplier': 2, 'targetPocketId': 0}, 'Shot', 1],
    [7, 'Reveal', 'BilliardsRevealProp', .468, .234,
     {'$type': 'RevealEffect', 'durationShots': 0, 'rateMultiplier': 1, 'targetPocketId': 0}, 'Level', 1],
    [8, 'Preview', 'BilliardsPreviewProp', .468, .234,
     {'$type': 'PreviewEffect', 'durationShots': 1, 'rateMultiplier': 1, 'targetPocketId': 0}, 'Shot', 1],
    [9, 'AddShot', 'BilliardsAddShotProp', .468, .234,
     {'$type': 'AddShotEffect', 'durationShots': 0, 'rateMultiplier': 1, 'targetPocketId': 0}, 'Level', 1],
]
prop_rows.extend(campaign['prop_configs'])
table('Prop',[('id','int'),('propKind','PropKind'),('prefabLocation','string'),('visualDiameter','float'),('triggerRadius','float'),('effectParams','PropEffect'),('useScope','UseScope'),('useLimit','int')],prop_rows)
table('Physics',[('id','int'),('simulationStep','float'),('ballRestitution','float'),('railRestitution','float'),('geometryRestitution','float'),('contactTolerance','float'),('stopSpeed','float'),('maxEventIterations','int'),('predictionBudget','int'),('decelerationModel','DecelerationModel')],[[1,1/120,.6,1,1,.0001,.015,64,10000,'ShotDistance']])
table('LevelBall',[('id','int'),('levelId',ref('Level')),('ballConfigId',ref('Ball')),('spawnPosition','vector2'),('spawnRadius','float')],campaign['balls'])
table('LevelPocket',[('id','int'),('levelId',ref('Level')),('slot','PocketSlot'),('initialState','PocketState')],campaign['pockets'])
table('LevelProp',[('id','int'),('levelId',ref('Level')),('propConfigId',ref('Prop')),('position','vector2'),('triggerMode','TriggerMode')],campaign['props'])
table('LevelGeometry',[('id','int'),('levelId',ref('Level')),('shape','Shape'),('position','vector2'),('rotation','float'),('uniformScale','float'),('conditionKind','ConditionKind'),('tolerance','float'),('allowedBallKind','BallKind'),('effectBinding','EffectBinding')],campaign['geometries'])
terrain_configs = [
    [1, 'JumpPad', 'BilliardsJumpPad', 0.8, 1.15, True, True],
    [2, 'Tunnel', 'BilliardsTunnel', 1.0, 1.0, True, True],
    [3, 'Pit', 'BilliardsPit', 1.2, 1.0, False, False],
    [4, 'ReverseBelt', 'BilliardsReverseBelt', 1.0, 1.0, True, True],
    [5, 'Teleporter', 'BilliardsTeleporter', 0.1, 1.0, True, True],
]
table('Terrain',[('id','int'),('terrainKind','TerrainKind'),('prefabLocation','string'),('defaultDuration','float'),('defaultSpeedMultiplier','float'),('inheritVelocity','bool'),('inheritRotation','bool')],terrain_configs)
table('LevelTerrain',[('id','int'),('levelId',ref('Level')),('terrainConfigId',ref('Terrain')),('position','vector2'),('size','vector2'),('direction','vector2'),('triggerRadius','float'),('duration','float'),('exitId','int')],campaign['terrains'])
tables.save(root / 'Datas/__tables__.xlsx')
indent(schema)
(root / 'Defines/game.xml').write_text(tostring(schema,encoding='unicode'),encoding='utf-8')
reference_path = Path(__file__).resolve().parents[1] / 'Docx/功能开发/数学桌球-二十关候选参考解.json'
reference_path.write_text(json.dumps(dict(angleConvention='+X=0 degrees, counterclockwise',
                                         status='candidate_unverified', levels=campaign['references']),
                                    ensure_ascii=False, indent=2), encoding='utf-8')
print(f"{len(campaign['levels'])} 关源配置、类型定义与候选参考解已写入；未运行物理回放。")
