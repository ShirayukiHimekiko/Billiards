"""Exact geometric pixel assets only; no game implementation or generated-art editing."""
from pathlib import Path
import csv
import math
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
SPRITES = ROOT / 'sprites'
PREVIEWS = ROOT / 'previews'
NAVY = '#17263b'
IVORY = '#fff2d1'
CYAN = '#73dedb'
CORAL = '#f9847d'
GOLD = '#ebc774'
MADE = []

def save(name, image, size):
    image = image.resize(size, Image.Resampling.NEAREST)
    image.save(SPRITES / (name + '.png'))
    MADE.append((name, image))

def blank(size):
    return Image.new('RGBA', size, (0,0,0,0))

# Samples are visual guides, never a replacement for arbitrary level geometry.
image = blank((64,64))
draw = ImageDraw.Draw(image)
draw.line([(32,2),(6,47),(57,47),(32,2)], fill=NAVY, width=3)
draw.line([(32,2),(6,47),(57,47),(32,2)], fill=GOLD, width=1)
save('triangle_target', image, (256,256))

for name, color, radius in [('hint_inscribed',CYAN,15),('hint_circumscribed',GOLD,30)]:
    image=blank((64,64))
    draw=ImageDraw.Draw(image)
    for angle in range(0,360,30):
        points=[(round(31.5+radius*math.cos(math.radians(t))),round(31.5+radius*math.sin(math.radians(t)))) for t in range(angle,angle+19,2)]
        draw.line(points,fill=color,width=1)
    save(name,image,(256,256))

image=blank((64,16))
draw=ImageDraw.Draw(image)
for x in range(3,50,8):
    draw.rectangle((x,7,x+4,8),fill=IVORY)
draw.line([(47,2),(58,7),(47,13)],fill=NAVY,width=3)
draw.line([(47,2),(58,7),(47,13)],fill=IVORY,width=1)
save('aim_arrow',image,(128,32))

for name,color,plus in [('icon_grow',CYAN,True),('icon_shrink',CORAL,False)]:
    image=blank((16,16))
    draw=ImageDraw.Draw(image)
    draw.rectangle((2,6,13,9),fill=NAVY)
    draw.rectangle((3,7,12,8),fill=color)
    if plus:
        draw.rectangle((6,2,9,13),fill=NAVY)
        draw.rectangle((7,3,8,12),fill=color)
        draw.rectangle((3,7,12,8),fill=color)
    save(name,image,(32,32))

for name,color,used in [('icon_success',GOLD,False),('icon_failure',CORAL,True)]:
    image=blank((32,32))
    draw=ImageDraw.Draw(image)
    draw.polygon([(10,3),(21,3),(28,10),(28,21),(21,28),(10,28),(3,21),(3,10)],fill=NAVY,outline=color)
    if used:
        draw.line([(10,10),(21,21)],fill=color,width=3)
        draw.line([(21,10),(10,21)],fill=color,width=3)
    else:
        draw.line([(8,16),(13,21),(23,11)],fill=color,width=3)
    save(name,image,(64,64))

for name, pressed in [('button_normal',False),('button_pressed',True)]:
    image=blank((32,12))
    draw=ImageDraw.Draw(image)
    draw.rectangle((1,1,30,10),fill=NAVY)
    draw.rectangle((2,2,29,9),fill='#8c5635' if pressed else '#b47b46')
    draw.line([(3,2),(28,2)],fill='#67422e' if pressed else GOLD,width=1)
    draw.line([(2,3),(2,8)],fill='#67422e' if pressed else '#d9a568',width=1)
    draw.line([(3,9),(29,9),(29,3)],fill='#62432f',width=1)
    save(name,image,(128,48))

image=blank((96,12))
draw=ImageDraw.Draw(image)
draw.rectangle((0,0,95,11),fill=NAVY)
draw.rectangle((1,1,94,10),outline=GOLD,width=1)
draw.rectangle((4,3,91,8),fill='#213c53')
save('power_frame',image,(192,24))
image=blank((88,6))
draw=ImageDraw.Draw(image)
draw.rectangle((0,0,87,5),fill='#309caa')
draw.rectangle((0,0,87,3),fill=CYAN)
draw.line((0,0,87,0),fill='#baf2e6',width=1)
save('power_fill',image,(176,12))

for name,used in [('shot_available',False),('shot_used',True)]:
    image=blank((16,16))
    draw=ImageDraw.Draw(image)
    draw.ellipse((2,2,13,13),fill=NAVY)
    draw.ellipse((3,3,12,12),fill='#435a6b' if used else IVORY)
    draw.arc((3,3,12,12),10,100,fill='#243a51' if used else '#d3c2a1',width=2)
    draw.rectangle((5,4,6,5),fill='#607389' if used else '#fffef1')
    save(name,image,(32,32))

# Manifest preserves the skill's required columns, adds honest delivery status.
manifest=ROOT/'design'/'assets.csv'
with manifest.open(encoding='utf-8-sig',newline='') as f:
    reader=csv.DictReader(f)
    rows=list(reader)
    fields=list(reader.fieldnames)
for field in ['method','status','file']:
    if field not in fields:
        fields.append(field)
ready={name for name,_ in MADE}
for row in rows:
    row['method']='pixel_geometry' if row['id'] in ready else 'image_generation'
    row['status']='ready' if row['id'] in ready else 'generating_builtin_imagegen'
    row['file']='sprites/'+row['id']+'.png' if row['id'] in ready else ''
with manifest.open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fields)
    writer.writeheader()
    writer.writerows(rows)

font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
font_small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17)
labels=['三角形目标示例','内切提示圆','外接提示圆','瞄准箭头','变大状态','变小状态','成功反馈','失败反馈']
sheet=Image.new('RGB',(1200,640),'#101b2b')
draw=ImageDraw.Draw(sheet)
draw.text((28,20),'数学桌球 · 第一批像素几何与界面素材',font=font,fill=IVORY)
for i,((name,asset),label) in enumerate(zip(MADE,labels)):
    col,row=i%4,i//4
    x,y=20+col*295,75+row*275
    draw.rectangle((x,y,x+278,y+252),fill='#1c3047',outline='#344963',width=2)
    preview=asset.copy()
    preview.thumbnail((190,190),Image.Resampling.NEAREST)
    sheet.paste(preview,(x+(278-preview.width)//2,y+12+(190-preview.height)//2),preview)
    draw.text((x+15,y+211),label,font=font,fill=IVORY)
    draw.text((x+15,y+239),f'{asset.width} × {asset.height}',font=font_small,fill='#aac1cd')
sheet.save(PREVIEWS/'第一批资源预览.png')
print('14 exact pixel assets written; generated artwork being prepared separately.')
