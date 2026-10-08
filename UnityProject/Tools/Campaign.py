"""构造渐进式二十关作者数据与候选解；通关证据由实际世界回放产生。"""
import math


LEVEL_SPECS = [
    (1, '第一次进洞', '基础', '瞄准黑球，按住左键蓄力，松开推球进洞。', 3),
    (2, '力度练习', '基础', '距离变远了，增加蓄力，让黑球到达洞口。', 3),
    (3, '碰撞方向', '基础', '瞄准黑球朝向洞口的反侧，改变碰撞后的方向。', 4),
    (4, '借边反弹', '进阶', '将黑球推向右侧桌边，借一次反弹进入上中洞。', 4),
    (5, '两球顺序', '进阶', '先上后下：每个洞只能接收一颗黑球。', 5),
    (6, '内切开门', '图形入门', '让白球与三角形内切，解锁上中洞后进洞。', 5),
    (7, '外接开门', '图形入门', '匹配三角形的外接圆，解锁上中洞后进洞。', 5),
    (8, '图形赠礼', '图形进阶', '先完成下方图形获得翻转，再匹配上方图形解锁。', 6),
    (9, '解锁与反弹', '综合', '先解锁并反弹进上中洞，再解锁另一目标进下中洞。', 7),
    (10, '综合谜题', '挑战', '先反弹进上中洞，再左下、右下，结合翻转解锁。', 9),
    (11, '双向借边', '路线进阶', '两次借边，分别送入上中洞、下中洞，保留下一杆位置。', 8),
    (12, '三球布阵', '路线进阶', '规划三球顺序，将黑球送入三个不同的洞。', 8),
    (13, '双门分工', '图形进阶', '分别匹配内切和外接，解锁两条进洞路线。', 8),
    (14, '先解后打', '图形进阶', '先完成两个解锁目标，再利用保留的进度击球入洞。', 9),
    (15, '借边过门', '综合进阶', '先匹配图形并借边进洞，再规划第二条解锁路线。', 9),
    (16, '缩小穿关', '道具进阶', '本杆先触发缩小，完成后续尺寸目标，再处理另一颗球。', 9),
    (17, '再度翻转', '道具进阶', '按顺序触发两个翻转目标，恢复增长后解锁球洞。', 10),
    (18, '四球调度', '路线挑战', '四球分配四洞，考虑路线避让与下一杆停球位置。', 10),
    (19, '连锁开洞', '综合挑战', '分阶段解锁、翻转并进洞，为后续黑球保留路线。', 11),
    (20, '终局规划', '最终挑战', '结合图形、翻转和四球洞位分配，完成整局规划。', 12),
]
SLOTS = ['TopLeft', 'TopMiddle', 'TopRight', 'BottomLeft', 'BottomMiddle', 'BottomRight']
SPAWN_RADIUS = .28
BLACK_RADIUS = .28
RADIUS_MIN = .15
RADIUS_MAX = 1.6
GROW_RATE_MIN = .06
GROW_RATE_MAX = .18
SHRINK_RATE = .12


def vector(position):
    """转换作者坐标到 Luban vector2 JSON。"""
    return dict(x=position[0], y=position[1])


def direction(origin, target):
    """根据作者路线计算单位方向。"""
    dx, dy = target[0]-origin[0], target[1]-origin[1]
    length = math.hypot(dx, dy)
    return (dx/length, dy/length)


def along(origin, heading, distance):
    """沿路线摆放球或目标。"""
    return (origin[0]+heading[0]*distance, origin[1]+heading[1]*distance)


def shot_time(distance, power):
    """按 ShotDistance 公式推导未碰撞路线的到达时间。"""
    speed = 4+4*power
    shot_distance = 4+14*power
    if distance < 0 or distance > shot_distance:
        raise ValueError(f'路程 {distance} 超出力度 {power} 的基准路程 {shot_distance}。')
    deceleration = speed*speed/(2*shot_distance)
    return (speed-math.sqrt(speed*speed-2*deceleration*distance))/deceleration


def grow_rate(power):
    """按运行时公式返回本杆每秒半径增长量。"""
    return GROW_RATE_MIN+(GROW_RATE_MAX-GROW_RATE_MIN)*power


def radius_on_path(start_radius, distance, power, flip_distance=None, reflip_distance=None):
    """按增长、缩小、恢复增长的连续区段推导指定路程处半径。"""
    time = shot_time(distance, power)
    rate = grow_rate(power)

    if flip_distance is None or distance <= flip_distance:
        return min(RADIUS_MAX, start_radius+rate*time)

    flip_time = shot_time(flip_distance, power)
    radius = min(RADIUS_MAX, start_radius+rate*flip_time)

    if reflip_distance is None or distance <= reflip_distance:
        return max(RADIUS_MIN, radius-SHRINK_RATE*(time-flip_time))

    reflip_time = shot_time(reflip_distance, power)
    radius = max(RADIUS_MIN, radius-SHRINK_RATE*(reflip_time-flip_time))
    return min(RADIUS_MAX, radius+rate*(time-reflip_time))


def upward_cut_aim(origin, target, power, start_radius=SPAWN_RADIUS):
    """按增长半径迭代构造从目标正下方斜碰的候选瞄准点，不执行物理回放。"""
    aim = (target[0], target[1]-start_radius-BLACK_RADIUS)
    for _ in range(8):
        radius = radius_on_path(start_radius, math.dist(origin, aim), power)
        aim = (target[0], target[1]-radius-BLACK_RADIUS)
    return aim


def contact_state(origin, target, power, start_radius=SPAWN_RADIUS,
                  flip_distance=None, reflip_distance=None):
    """用连续半径函数求正碰时的候选球心、半径和已行进路程。"""
    heading = direction(origin, target)
    center_distance = math.dist(origin, target)
    upper = min(center_distance, 4+14*power)

    def gap(travel):
        radius = radius_on_path(start_radius, travel, power, flip_distance, reflip_distance)
        return travel+radius+BLACK_RADIUS-center_distance

    if gap(upper) < 0:
        raise ValueError('候选路线在本杆基准路程内无法接触目标球。')

    lower = 0.0
    for _ in range(64):
        middle = (lower+upper)*.5
        if gap(middle) >= 0:
            upper = middle
        else:
            lower = middle

    travel = upper
    radius = radius_on_path(start_radius, travel, power, flip_distance, reflip_distance)
    return along(origin, heading, max(0, travel-.00005)), radius, travel


def cue(origin, target, power, expected):
    """记录玩家可输入的候选方向、力度及事件顺序。"""
    heading = direction(origin, target)
    aim_point = target
    # 反弹构造使用桌外虚拟洞口；玩家实际瞄准点必须落在台面内。
    if not (-8 < target[0] < 8 and -4 < target[1] < 4):
        exit_distance = min((bound-origin[axis])/heading[axis]
                            for axis, bounds in enumerate([(-8, 8), (-4, 4)])
                            for bound in bounds
                            if abs(heading[axis]) > 1e-9 and (bound-origin[axis])/heading[axis] > 0)
        aim_point = along(origin, heading, min(1, exit_distance*.5))
    return dict(angleDegrees=round(math.degrees(math.atan2(heading[1], heading[0])), 6),
                power=power, aimPoint=vector(aim_point), expected=expected)


def create_campaign():
    """构造二十关数据；几何尺寸根据候选路线的半径反向确定。"""
    balls, pockets, props, geometries, references = [], [], [], [], []
    triangle = {'$type':'Triangle', 'a':vector((0,1.2)),
                'b':vector((-1.0392305,-.6)), 'c':vector((1.0392305,-.6))}

    def layout(level, white, blacks, usable_slots, white_config=1, locked=None):
        """基础关直接开洞，图形关锁洞，六洞明细均完整保留。"""
        balls.append([level*100+1, level, white_config, vector(white), SPAWN_RADIUS])
        for index, position in enumerate(blacks, 2):
            balls.append([level*100+index, level, 2, vector(position), BLACK_RADIUS])
        for index, slot in enumerate(SLOTS, 1):
            requires_unlock = level > 5 if locked is None else locked
            state = ('Locked' if requires_unlock else 'Unlocked') if slot in usable_slots else 'Disabled'
            pockets.append([level*100+index, level, slot, state])

    def unlock(level, slot):
        """引用本关稳定球洞 ID。"""
        return {'$type':'UnlockPocket', 'pocketPlacementId':level*100+SLOTS.index(slot)+1}

    def geometry(level, number, position, condition, radius, effect, tolerance=.06, rotation=0):
        """由目标圆的半径构造等边三角形。"""
        geometries.append([level*100+number, level, triangle, vector(position), rotation,
                           radius/(.6 if condition=='Inscribed' else 1.2), condition,
                           tolerance, 'White', effect])

    def reference(level, shots):
        """保存候选解，不标记为已经验证。"""
        references.append(dict(levelId=level, status='candidate_unverified', shots=shots))

    # 前五关不放置图形和道具，每关只增加一个教学点。
    layout(1, (0,0), [(0,2.7)], ['TopMiddle'])
    reference(1, [cue((0,0), (0,4), .25, '黑球进入上中洞')])
    layout(2, (0,-2.5), [(0,1)], ['TopMiddle'])
    reference(2, [cue((0,-2.5), (0,4), .65, '黑球进入上中洞')])
    layout(3, (-2,-1), [(0,2)], ['TopMiddle'])
    cut_aim = upward_cut_aim((-2,-1), (0,2), .60)
    reference(3, [cue((-2,-1), cut_aim, .60, '按增长后的接触半径斜碰，黑球沿 +Y 进入上中洞')])

    bank_start = (0,0)
    # 反射的是黑球球心的边界，瞄准中洞入口而非跨越桌边后的洞心。
    bank_heading = direction(bank_start, (15.44,3.72))
    bank_ball = along(bank_start, bank_heading, 3.5)
    bank_white = bank_start
    layout(4, bank_white, [bank_ball], ['TopMiddle'])
    reference(4, [cue(bank_white, bank_ball, 1, '黑球撞右侧桌边后进入上中洞')])
    layout(5, (0,0), [(0,2.5),(0,-2.5)], ['TopMiddle','BottomMiddle'])
    after_top, _, _ = contact_state((0,0), (0,2.5), .30)
    reference(5, [cue((0,0), (0,4), .30, '第一颗黑球进入上中洞'),
                  cue(after_top, (0,-4), .55, '第二颗黑球进入下中洞')])

    # 第六、七关使用同样的简单路线，仅新增内切／外接条件。
    for level, condition, power in [(6,'Inscribed',.35), (7,'Circumscribed',.50)]:
        layout(level, (0,-2), [(0,2.6)], ['TopMiddle'])
        radius = radius_on_path(SPAWN_RADIUS, 2, power)
        geometry(level, 1, (0,0), condition, radius, unlock(level,'TopMiddle'), .08)
        reference(level, [cue((0,-2), (0,4), power, '完成图形、解锁上中洞并将黑球推进洞')])

    # 两个目标分开，避免前一个目标完成前先撞上后一个几何门。
    layout(8, (0,-2.4), [(0,2.8)], ['TopMiddle'])
    power = .30
    props.append([801,8,2,vector((0,-.8)),'Geometry'])
    geometry(8,1,(0,-.8),'Inscribed',radius_on_path(SPAWN_RADIUS,1.6,power),
             {'$type':'TriggerProp','propPlacementId':801},.05)
    geometry(8,2,(0,.6),'Inscribed',radius_on_path(SPAWN_RADIUS,3,power,1.6),
             unlock(8,'TopMiddle'))
    reference(8,[cue((0,-2.4),(0,4),power,'完成下方图形翻转为缩小，再完成上方图形解锁并进洞')])

    # 第九关按增长半径构造两个目标；上方内切三角形朝下以容纳更大的尺寸。
    bank_end, bank_end_radius, _ = contact_state(bank_start,bank_ball,1)
    second_heading = direction(bank_end,(0,-4))
    second_ball = along(bank_end,second_heading,4.5)
    layout(9,bank_start,[bank_ball,second_ball],['TopMiddle','BottomMiddle'])
    geometry(9,1,along(bank_start,bank_heading,1),'Inscribed',
             radius_on_path(SPAWN_RADIUS,1,1),unlock(9,'TopMiddle'),rotation=180)
    geometry(9,2,along(bank_end,second_heading,3),'Circumscribed',
             radius_on_path(bank_end_radius,3,.70),unlock(9,'BottomMiddle'))
    reference(9,[cue(bank_start,bank_ball,1,'完成第一个图形；黑球撞右边后进入上中洞'),
                 cue(bank_end,(0,-4),.70,'完成第二个图形；另一颗黑球进入下中洞')])

    # 最后一关按前一杆候选终态摆放下一颗黑球，引用完整三杆路线。
    start, first_ball, first_power = bank_start, bank_ball, 1
    first_end, first_end_radius, _ = contact_state(
        start,first_ball,first_power,SPAWN_RADIUS,.25)
    left_entry, right_entry = (-7.72,-3.72), (7.72,-3.72)
    left_heading = direction(first_end,left_entry)
    left_ball = along(left_entry,left_heading,-2)
    second_power = .60
    second_end, second_end_radius, _ = contact_state(
        first_end,left_ball,second_power,first_end_radius)
    right_heading = direction(second_end,right_entry)
    # 增长中的白球需要更大库边间隙，将最后一颗黑球沿入洞方向移向桌内。
    right_ball = along(right_entry,right_heading,-6)
    layout(10,start,[first_ball,left_ball,right_ball],['TopMiddle','BottomLeft','BottomRight'],1)
    flip_center = along(start,bank_heading,.25)
    props.append([1001,10,2,vector(flip_center),'Geometry'])
    geometry(10,1,flip_center,'Inscribed',radius_on_path(SPAWN_RADIUS,.25,first_power),
             {'$type':'TriggerProp','propPlacementId':1001},.05,180)
    geometry(10,2,along(start,bank_heading,1.7),'Inscribed',
             radius_on_path(SPAWN_RADIUS,1.7,first_power,.25),
             unlock(10,'TopMiddle'),.06,180)
    geometry(10,3,along(first_end,left_heading,5),'Circumscribed',
             radius_on_path(first_end_radius,5,second_power),
             unlock(10,'BottomLeft'))
    third_power = .7
    geometry(10,4,along(second_end,right_heading,4),'Circumscribed',
             radius_on_path(second_end_radius,4,third_power),
             unlock(10,'BottomRight'))
    reference(10,[cue(start,first_ball,first_power,'获得翻转、解锁上中洞，第一颗黑球撞右边后入洞'),
                  cue(first_end,left_entry,second_power,'新杆恢复增长，完成左侧外接目标，第二颗黑球进左下洞'),
                  cue(second_end,right_entry,third_power,'新杆恢复增长，完成右侧外接目标，第三颗黑球进右下洞')])

    entries = dict(TopLeft=(-7.72,3.72), TopMiddle=(0,3.72), TopRight=(7.72,3.72),
                   BottomLeft=(-7.72,-3.72), BottomMiddle=(0,-3.72), BottomRight=(7.72,-3.72))

    def route(origin, target, distance, power, start_radius=SPAWN_RADIUS,
              flipped=False, flip_distance=.7, reflip_distance=None):
        """按本杆真实起始半径记录正碰路线和候选接触终态。"""
        heading = direction(origin,target)
        ball = along(origin,heading,distance)
        first_flip = flip_distance if flipped else None
        end, end_radius, contact_distance = contact_state(
            origin, ball, power, start_radius, first_flip, reflip_distance)
        return dict(origin=origin,target=target,heading=heading,ball=ball,end=end,power=power,
                    startRadius=start_radius,endRadius=end_radius,contactDistance=contact_distance,
                    flipped=flipped,flipDistance=first_flip,reflipDistance=reflip_distance)

    def free_route(origin, target, power, start_radius=SPAWN_RADIUS,
                   flipped=False, flip_distance=.7, reflip_distance=None):
        """记录没有黑球碰撞、沿基准路程自然停止的候选路线。"""
        heading = direction(origin,target)
        distance = 4+14*power
        first_flip = flip_distance if flipped else None
        return dict(origin=origin,target=target,heading=heading,
                    end=along(origin,heading,distance),power=power,
                    startRadius=start_radius,
                    endRadius=radius_on_path(start_radius,distance,power,first_flip,reflip_distance),
                    contactDistance=distance,flipped=flipped,
                    flipDistance=first_flip,reflipDistance=reflip_distance)

    def radius_on_route(path, distance):
        """按本杆已设计的翻转时序推导图形条件尺寸。"""
        return radius_on_path(path['startRadius'],distance,path['power'],
                              path['flipDistance'],path['reflipDistance'])

    def route_goal(level, number, path, distance, condition, effect):
        """布置路线目标，内切朝向桌面较宽的一侧。"""
        center = along(path['origin'],path['heading'],distance)
        rotation = 180 if center[1] > 0 and condition == 'Inscribed' else 0
        geometry(level,number,center,condition,radius_on_route(path,distance),effect,.08,rotation)

    def flip_goal(level, number, prop_number, path, distance, condition='Circumscribed'):
        """两个翻转目标使用各自独立的整关一次道具。"""
        center = along(path['origin'],path['heading'],distance)
        prop_id = level*100+prop_number
        props.append([prop_id,level,2,vector(center),'Geometry'])
        route_goal(level,number,path,distance,condition,{'$type':'TriggerProp','propPlacementId':prop_id})

    def record_routes(level, paths, slots, descriptions, locked=False):
        """按完整候选多杆路线生成关卡摆放及输入记录。"""
        layout(level,paths[0]['origin'],[p['ball'] for p in paths],slots,1,locked)
        reference(level,[cue(p['origin'],p['target'],p['power'],description)
                         for p,description in zip(paths,descriptions)])

    # 第十一关：相反两侧的反弹路线；目标点是黑球球心反射后的虚拟洞口。
    first = route((0,0),(15.44,3.72),3.5,1)
    second = route(first['end'],(-15.44,-3.72),4.5,1,first['endRadius'])
    record_routes(11,[first,second],['TopMiddle','BottomMiddle'],
                  ['借右侧桌边进入上中洞','借左侧桌边进入下中洞'])

    first = route((0,-.3),entries['TopLeft'],4,.8)
    second = route(first['end'],entries['BottomRight'],5,.85,first['endRadius'])
    third = route(second['end'],entries['TopRight'],4,.75,second['endRadius'])
    record_routes(12,[first,second,third],['TopLeft','BottomRight','TopRight'],
                  ['第一颗球进左上洞','第二颗球进右下洞','第三颗球进右上洞'])

    first = route((0,0),entries['TopLeft'],4,.7)
    second = route(first['end'],entries['BottomRight'],5,.8,first['endRadius'])
    record_routes(13,[first,second],['TopLeft','BottomRight'],
                  ['完成内切，进左上洞','完成外接，进右下洞'],True)
    route_goal(13,1,first,1.2,'Inscribed',unlock(13,'TopLeft'))
    route_goal(13,2,second,2.2,'Circumscribed',unlock(13,'BottomRight'))

    # 第十四关独立的准备杆：无碰撞基准路程为 6.8，目标在准备路线依次解锁。
    preparation = free_route((-4,0),(3,0),.2)
    preparation_end = preparation['end']
    first = route(preparation_end,entries['TopRight'],3,.65,preparation['endRadius'])
    second = route(first['end'],entries['BottomLeft'],8,.9,first['endRadius'])
    record_routes(14,[first,second],['TopRight','BottomLeft'],
                  ['利用已经解锁的右上洞进球','利用已经解锁的左下洞进球'],True)
    # 出生点必须保持准备杆的起点，不能把参考解的中间态作为出生点。
    next(row for row in balls if row[0] == 1401)[3] = vector((-4,0))
    route_goal(14,1,preparation,2,'Inscribed',unlock(14,'TopRight'))
    route_goal(14,2,preparation,4,'Circumscribed',unlock(14,'BottomLeft'))
    references[-1]['shots'].insert(0,cue((-4,0),(3,0),.2,'先完成两个目标，解锁右上与左下洞；本杆不进黑球'))

    first = route((0,0),(15.44,3.72),4,1)
    second = route(first['end'],entries['BottomLeft'],5,.8,first['endRadius'])
    record_routes(15,[first,second],['TopMiddle','BottomLeft'],
                  ['匹配内切后借右边进上中洞','匹配外接后进左下洞'],True)
    route_goal(15,1,first,1.5,'Inscribed',unlock(15,'TopMiddle'))
    route_goal(15,2,second,2.5,'Circumscribed',unlock(15,'BottomLeft'))

    first = route((0,-.4),entries['TopLeft'],4,.7,SPAWN_RADIUS,True)
    second = route(first['end'],entries['BottomRight'],5,.8,first['endRadius'])
    record_routes(16,[first,second],['TopLeft','BottomRight'],
                  ['先缩小，再匹配目标解锁左上洞并进球','新杆增长，解锁右下洞并进球'],True)
    flip_goal(16,1,1,first,.7)
    route_goal(16,2,first,2,'Circumscribed',unlock(16,'TopLeft'))
    route_goal(16,3,second,2.2,'Circumscribed',unlock(16,'BottomRight'))

    first = route((0,-3),entries['TopMiddle'],6.2,.65,SPAWN_RADIUS,True,1.1,3.3)
    second = route(first['end'],entries['BottomRight'],4.5,.9,first['endRadius'])
    record_routes(17,[first,second],['TopMiddle','BottomRight'],
                  ['依次缩小、恢复增长，再解锁上中洞并进球','匹配外接，进右下洞'],True)
    flip_goal(17,1,1,first,1.1)
    flip_goal(17,2,2,first,3.3)
    route_goal(17,3,first,4.9,'Circumscribed',unlock(17,'TopMiddle'))
    route_goal(17,4,second,2.4,'Circumscribed',unlock(17,'BottomRight'))

    first = route((0,-.3),entries['TopLeft'],4,.8)
    second = route(first['end'],entries['BottomRight'],5,.85,first['endRadius'])
    third = route(second['end'],entries['TopRight'],4,.75,second['endRadius'])
    fourth = route(third['end'],entries['BottomLeft'],6,.9,third['endRadius'])
    record_routes(18,[first,second,third,fourth],['TopLeft','BottomRight','TopRight','BottomLeft'],
                  ['先左上','再右下','然后右上','最后左下'])

    first = route((0,-.4),entries['TopLeft'],4,.7,SPAWN_RADIUS,True)
    second = route(first['end'],entries['BottomRight'],5,.8,first['endRadius'])
    third = route(second['end'],entries['TopRight'],5,.85,second['endRadius'])
    record_routes(19,[first,second,third],['TopLeft','BottomRight','TopRight'],
                  ['翻转、解锁左上洞并进球','解锁右下洞并进球','解锁右上洞并进球'],True)
    flip_goal(19,1,1,first,.7)
    route_goal(19,2,first,2,'Circumscribed',unlock(19,'TopLeft'))
    route_goal(19,3,second,2.2,'Inscribed',unlock(19,'BottomRight'))
    route_goal(19,4,third,3,'Circumscribed',unlock(19,'TopRight'))

    # 先用独立准备杆完成两次翻转及首洞解锁，再从真实停球点开始反弹路线。
    preparation = free_route((0,-3),(0,3),.1,SPAWN_RADIUS,True,1,2.3)
    preparation_end = preparation['end']
    first = route(preparation_end,(15.44,-3.72),3.5,1,preparation['endRadius'])
    second = route(first['end'],entries['BottomLeft'],8.2,.9,first['endRadius'])
    third = route(second['end'],entries['TopLeft'],5,.9,second['endRadius'])
    fourth = route(third['end'],entries['BottomRight'],8,1,third['endRadius'])
    record_routes(20,[first,second,third,fourth],['BottomMiddle','BottomLeft','TopLeft','BottomRight'],
                  ['完成外接解锁右下洞，借右边反弹进下中洞','完成内切、解锁左下洞并进球','完成外接，解锁左上洞并进球','利用之前的解锁进度，最后一颗球进入右下洞'],True)
    next(row for row in balls if row[0] == 2001)[3] = vector((0,-3))
    references[-1]['shots'].insert(0,cue((0,-3),(0,3),.1,'准备杆依次缩小、恢复增长并解锁下中洞'))
    flip_goal(20,1,1,preparation,1)
    flip_goal(20,2,2,preparation,2.3)
    route_goal(20,3,preparation,3.5,'Circumscribed',unlock(20,'BottomMiddle'))
    route_goal(20,4,second,6,'Inscribed',unlock(20,'BottomLeft'))
    route_goal(20,5,third,3,'Circumscribed',unlock(20,'TopLeft'))
    route_goal(20,6,first,math.dist(first['origin'],first['end']),'Circumscribed',unlock(20,'BottomRight'))
    return dict(levels=LEVEL_SPECS, balls=balls, pockets=pockets, props=props,
                geometries=geometries, references=references)
