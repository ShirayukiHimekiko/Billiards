"""Copy originals, package display-size copies and a static scale preview; no game code."""
from pathlib import Path
import csv
import json
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parent.parent
SPRITES=ROOT/'sprites'
RAW=ROOT/'raw'
PREVIEWS=ROOT/'previews'
PREVIEWS.mkdir(exist_ok=True)
SIZES={
 'board_background':(1280,720),
 'ball_normal':(128,128),'ball_inverse':(128,128),
 'toggle_active':(128,128),'toggle_used':(128,128),
 'ui_panel':(256,96)
}
for name,size in SIZES.items():
    source=RAW/(name+'.png')
    if not source.exists():
        raise FileNotFoundError(source)
    image=Image.open(source)
    if name=='board_background':
        image=image.convert('RGB').resize(size,Image.Resampling.NEAREST)
    else:
        image=image.convert('RGBA')
        alpha=image.getchannel('A')
        if alpha.getextrema()[0]!=0:
            raise ValueError(f'No transparent alpha: {name}')
        # Ignore almost invisible alpha specks when measuring the visible footprint.
        # Preserve the original pixels and alpha inside the resulting crop.
        bounds=alpha.point(lambda value: 255 if value >= 128 else 0).getbbox()
        if not bounds:
            raise ValueError(f'Empty alpha: {name}')
        cropped=image.crop(bounds)
        target=(112,112) if name.startswith('ball_') else ((104,104) if name.startswith('toggle_') else (248,88))
        cropped.thumbnail(target,Image.Resampling.NEAREST)
        image=Image.new('RGBA',size,(0,0,0,0))
        image.alpha_composite(cropped,((size[0]-cropped.width)//2,(size[1]-cropped.height)//2))
    image.save(SPRITES/(name+'.png'))

manifest=ROOT/'design'/'assets.csv'
with manifest.open(encoding='utf-8-sig',newline='') as f:
    reader=csv.DictReader(f)
    fields=list(reader.fieldnames)
    rows=list(reader)
labels={r['id']:r['role'] for r in rows}
for row in rows:
    asset=SPRITES/(row['id']+'.png')
    if not asset.exists():
        raise FileNotFoundError(asset)
    row['status']='ready'
    row['file']='sprites/'+asset.name
    if row['id'] in SIZES:
        row['method']='builtin_imagegen'
        row['size/ratio']=f'{SIZES[row["id"]][0]}x{SIZES[row["id"]][1]}'
with manifest.open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fields)
    writer.writeheader()
    writer.writerows(rows)

font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',16)
title=ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc',30)
ivory='#fff2d1'
catalog=Image.new('RGB',(1500,1240),'#101b2b')
draw=ImageDraw.Draw(catalog)
draw.text((30,22),'数学桌球 · 像素资源总览',font=title,fill=ivory)
draw.text((30,64),'深蓝桌面 / 象牙白球 / 青色变大 / 珊瑚红变小 / 金色目标 / 紫色道具',font=small,fill='#aac1cd')
for i,row in enumerate(rows):
    x,y=20+(i%5)*296,110+(i//5)*274
    draw.rectangle((x,y,x+278,y+252),fill='#1c3047',outline='#344963',width=2)
    image=Image.open(SPRITES/(row['id']+'.png')).convert('RGBA')
    thumb=image.copy()
    thumb.thumbnail((228,176),Image.Resampling.NEAREST)
    catalog.paste(thumb,(x+(278-thumb.width)//2,y+15+(176-thumb.height)//2),thumb)
    draw.text((x+12,y+195),row['role'],font=font,fill=ivory)
    draw.text((x+12,y+230),f'{image.width} x {image.height}   PNG',font=small,fill='#aac1cd')
catalog.save(PREVIEWS/'全部资源预览.png')

scene=Image.open(SPRITES/'board_background.png').convert('RGBA')
def paste(name,pos,size=None):
    image=Image.open(SPRITES/(name+'.png')).convert('RGBA')
    if size:
        image=image.resize(size,Image.Resampling.NEAREST)
    scene.alpha_composite(image,pos)

# Fixed artwork layout is a mockup, never a claim of implemented gameplay.
paste('hint_inscribed',(780,230))
paste('hint_circumscribed',(780,230))
paste('triangle_target',(780,230))
paste('ball_normal',(260,357))
paste('aim_arrow',(390,405),(160,40))
paste('toggle_active',(590,362),(104,104))
paste('ui_panel',(98,115),(400,150))
d=ImageDraw.Draw(scene)
d.text((135,138),'让圆贴合三角形',font=title,fill=ivory)
d.text((135,188),'常态：变大',font=font,fill='#73dedb')
paste('shot_available',(330,190))
paste('shot_available',(366,190))
paste('shot_used',(402,190))
paste('power_frame',(140,565))
paste('power_fill',(148,571),(110,12))
d=ImageDraw.Draw(scene)
d.text((140,603),'力量',font=font,fill=ivory)
paste('button_normal',(1010,573),(160,60))
d=ImageDraw.Draw(scene)
d.text((1064,587),'重开',font=font,fill=ivory)
scene.convert('RGB').save(PREVIEWS/'游戏画面组合示意.png')

checks=[]
for row in rows:
    image=Image.open(SPRITES/(row['id']+'.png'))
    expected=tuple(int(v) for v in row['size/ratio'].split(' /')[0].split('x'))
    assert image.size==expected,row['id']
    if row['id']!='board_background':
        assert image.mode=='RGBA',row['id']
        if row['id'] not in ('power_frame','power_fill'):
            assert image.getchannel('A').getextrema()[0]==0,row['id']
        assert image.getchannel('A').getbbox(),row['id']
    checks.append({'id':row['id'],'size':list(image.size),'mode':image.mode,'alpha_range':list(image.getchannel('A').getextrema()) if image.mode=='RGBA' else None,'status':'PASS'})
report={'asset_count':len(rows),'transparent_assets':17,'opaque_ui_rectangles':2,'backgrounds':1,'checks':checks,'scope':'File dimensions and transparency; previews are static artwork, not a running game.'}
(ROOT/'design'/'文件检查结果.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'assets':len(rows),'all_files_checked':'PASS','preview_count':2}))
