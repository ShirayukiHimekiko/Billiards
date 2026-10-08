// 代码原生工程图：同一份节点数据生成独立 SVG，可选用 sharp 生成 PNG。
const fs=require('node:fs'),path=require('node:path');
const out=path.join(__dirname,'..','流程图');fs.mkdirSync(out,{recursive:true});
const escape=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&apos;'}[c]));
const palette={blue:['#eaf2ff','#4274c4'],green:['#e5f5ee','#238064'],red:['#fff0ee','#bc564b'],amber:['#fff6dd','#ae8429'],gray:['#f1f4f8','#74849a']};
const n=(id,x,y,lines,color='blue',w=250,h=78,kind='box')=>({id,x,y,lines:Array.isArray(lines)?lines:[lines],color,w,h,kind});
const e=(from,to,label='',points=null,fromSide='bottom',toSide='top',dashed=false)=>({from,to,label,points,fromSide,toSide,dashed});
const anchor=(n,s)=>s==='left'?[n.x,n.y+n.h/2]:s==='right'?[n.x+n.w,n.y+n.h/2]:s==='top'?[n.x+n.w/2,n.y]:[n.x+n.w/2,n.y+n.h];
function render(name,title,subtitle,W,H,nodes,edges,notes=[]){
  const map=Object.fromEntries(nodes.map(v=>[v.id,v]));let parts=[];
  for(const edge of edges){
    const a=anchor(map[edge.from],edge.fromSide),b=anchor(map[edge.to],edge.toSide);
    const points=[a,...(edge.points|| (a[0]===b[0]||a[1]===b[1]?[]:[[a[0],(a[1]+b[1])/2],[b[0],(a[1]+b[1])/2]])),b];
    parts.push(`<path d="${points.map((p,i)=>`${i?'L':'M'} ${p[0]} ${p[1]}`).join(' ')}" fill="none" stroke="#63748a" stroke-width="2.2" marker-end="url(#arrow)"${edge.dashed?' stroke-dasharray="7 5"':''}/>`);
    if(edge.label){let index=0,len=0;for(let i=1;i<points.length;i++){let l=Math.hypot(points[i][0]-points[i-1][0],points[i][1]-points[i-1][1]);if(l>len){len=l;index=i;}}
      const w=edge.label.length*17+18,x=Math.max(w/2+12,Math.min(W-w/2-12,(points[index-1][0]+points[index][0])/2)),y=(points[index-1][1]+points[index][1])/2-9;
      parts.push(`<rect x="${x-w/2}" y="${y-18}" width="${w}" height="26" rx="5" fill="#f8fafd"/><text x="${x}" y="${y+1}" text-anchor="middle" font-size="16" fill="#506079">${escape(edge.label)}</text>`);
    }
  }
  for(const node of nodes){const [fill,stroke]=palette[node.color];
    if(node.kind==='decision')parts.push(`<polygon points="${node.x+node.w/2},${node.y} ${node.x+node.w},${node.y+node.h/2} ${node.x+node.w/2},${node.y+node.h} ${node.x},${node.y+node.h/2}" fill="${fill}" stroke="${stroke}" stroke-width="2"/>`);
    else parts.push(`<rect x="${node.x}" y="${node.y}" width="${node.w}" height="${node.h}" rx="12" fill="${fill}" stroke="${stroke}" stroke-width="2"/>`);
    node.lines.forEach((line,i)=>parts.push(`<text x="${node.x+node.w/2}" y="${node.y+node.h/2-(node.lines.length-1)*14+i*28+7}" text-anchor="middle" font-size="20" font-weight="${i===0?'600':'400'}" fill="#20334e">${escape(line)}</text>`));
    if(node.x<0||node.y<0||node.x+node.w>W||node.y+node.h>H)throw Error(`Node outside canvas: ${name}/${node.id}`);
  }
  notes.forEach((s,i)=>parts.push(`<text x="30" y="${H-48+i*27}" font-size="18" fill="#5b6c82">${escape(s)}</text>`));
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H+140}" viewBox="0 0 ${W} ${H+140}"><defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0 0L10 5L0 10Z" fill="#63748a"/></marker></defs><rect width="100%" height="100%" fill="#f8fafd"/><g font-family="Microsoft YaHei, Noto Sans CJK SC, sans-serif"><text x="30" y="43" font-size="29" font-weight="700" fill="#172b47">${escape(title)}</text><text x="30" y="77" font-size="18" fill="#5b6c82">${escape(subtitle)}</text><g transform="translate(0 100)">${parts.join('\n')}</g><text x="30" y="${H+127}" font-size="15" fill="#7b8797">数学桌球 · 简明版 · 2026-10-04</text></g></svg>`;
  fs.writeFileSync(path.join(out,name+'.svg'),svg,'utf8');
}
render('01-整体架构','游戏整体结构','明确每个部分的职责，统一管理游戏进程。',1100,900,[
 n('player',420,10,['玩家操作']),n('input',420,140,['瞄准 / 蓄力 / 出杆']),
 n('flow',420,280,['游戏流程','管理阶段、杆数和结果'],'amber'),
 n('ball',35,440,['白球运动','滚动和大小变化']),n('judge',420,440,['目标与道具判断','决定成功、翻转或失败']),
 n('ui',800,440,['界面提示','力量、杆数和状态']),n('result',420,620,['结果处理','成功 / 重试 / 重开'],'green')
],[e('player','input'),e('input','flow'),e('flow','ball'),e('flow','judge'),e('flow','ui'),e('judge','result')],['白球负责运动，判定负责检查，界面负责显示，游戏流程统一决定结果。']);
render('02-玩家功能流程','一局游戏怎样进行','每关3杆；内切或外接任意一种成立即可通关。',1100,1130,[
 n('start',410,10,['进入关卡','白球和道具准备好']),n('aim',410,130,['瞄准并蓄力']),n('shoot',410,250,['松开出杆','消耗1杆']),
 n('roll',410,380,['滚动 / 大小变化','接触道具时翻转']),n('win',790,380,['达到目标','立即通关'],'green'),
 n('fail',410,530,['出界或停下未成功','本杆失败'],'red'),n('left',395,670,['还有机会?'],'amber',280,100,'decision'),
 n('reset',35,680,['恢复出生点和道具','继续下一杆'],'gray',280),n('lose',410,880,['机会耗尽','关卡失败'],'red'),n('restart',790,880,['重开本关','恢复3杆'],'gray',280)
],[e('start','aim'),e('aim','shoot'),e('shoot','roll'),e('roll','win','完成目标',null,'right','left'),e('roll','fail'),e('fail','left'),e('left','reset','是',null,'left','right'),e('reset','aim','重试',[[20,719],[20,169]],'left','left'),e('left','lose','否'),e('lose','restart','重开',null,'right','left'),e('win','restart'),e('restart','start','重新开始',[[1080,919],[1080,49]],'right','right')],['蓄力取消不扣杆；失败不重复扣杆；第三杆仍可通关。']);
render('03-会话状态','游戏有哪些阶段','每个阶段只允许对应操作，避免重复出杆或重复结算。',1100,1030,[
 n('prepare',410,10,['准备关卡']),n('aim',410,140,['瞄准']),n('charge',410,290,['蓄力']),n('roll',410,440,['滚动']),
 n('win',790,440,['成功','查看结果 / 重开'],'green',280),n('fail',410,590,['本杆失败']),n('retry',35,750,['有机会：复位','回到瞄准'],'gray',280),n('over',410,750,['无机会：关卡失败','查看结果 / 重开'],'red')
],[e('prepare','aim'),e('aim','charge','按下'),e('charge','aim','取消',[[335,329],[335,179]],'left','left'),e('charge','roll','松开'),e('roll','win','达到目标',null,'right','left'),e('roll','fail','越界 / 未成功'),e('fail','retry','仍有机会'),e('fail','over','机会耗尽'),e('retry','aim','继续',[[20,789],[20,179]],'left','left')],['暂停时保留原位置和大小；恢复后继续。成功或关卡失败后，重开回到准备关卡。']);
render('04-连续事件处理','滚动期间检查什么','成功立即停球；翻转道具后继续滚动。',1100,1100,[
 n('roll',410,10,['白球滚动并改变大小']),n('out',395,150,['球体越界?'],'amber',280,100,'decision'),
 n('fail',35,160,['是：本杆失败'],'red',280),n('match',395,330,['完成内切或外接?'],'amber',280,100,'decision'),n('win',790,340,['是：立即通关'],'green',280),
 n('item',410,520,['接触可用道具时','保持大小并翻转变化方向']),n('stop',395,700,['走完路程?'],'amber',280,100,'decision'),
 n('final',410,900,['是：停止并最后检查','成功或本杆失败'])
],[e('roll','out'),e('out','fail','是',null,'left','right'),e('out','match','否'),e('match','win','是',null,'right','left'),e('match','item','否'),e('item','stop'),e('stop','final','是'),e('stop','roll','否：继续',[[20,750],[20,49]],'left','left')],['不能遗漏途中短暂贴合或高速接触；同刻冲突先出界，再成功，再道具，最后停止。']);
render('05-出杆道具时序','翻转道具怎样生效','道具只改变大小变化方向，不改变速度、方向和计划路程。',1100,880,[
 n('grow',410,10,['起始：常态','球逐渐变大']),n('touch',410,150,['球接触道具']),n('keep',410,290,['保留触发时的大小','不会突然变大或变小']),
 n('flip',410,430,['切换为逆态','球开始逐渐变小']),n('used',410,570,['道具本杆已使用','白球继续滚动']),n('reset',410,710,['下一杆','恢复常态和道具可用'],'gray')
],[e('grow','touch'),e('touch','keep'),e('keep','flip'),e('flip','used'),e('used','reset','本杆结束')]);
render('06-设计开发流程','设计和开发按什么顺序','每一步都有可检查的成果，再进入下一步。',1100,1170,[
 n('idea',410,10,['理解玩法目标']),n('base',410,145,['击球、运动和大小变化']),n('goal',410,280,['加入两种通关判定']),n('item',410,415,['加入翻转道具']),
 n('loop',410,550,['完成3杆循环和重开']),n('ui',410,685,['加入界面和提示']),n('check',395,820,['试玩和检查通过?'],'amber',280,100,'decision'),n('deliver',410,1000,['交付单关演示'],'green')
],[e('idea','base'),e('base','goal'),e('goal','item'),e('item','loop'),e('loop','ui'),e('ui','check'),e('check','deliver','是'),e('check','base','否：调整',[[80,870],[80,184]],'left','left')],['先保证关卡有解，再调操作手感；完成后检查流畅度、暂停、退出和重开。']);
const goalSvg=`<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="610" viewBox="0 0 1200 610"><rect width="1200" height="610" fill="#f8fafd"/><g font-family="Microsoft YaHei, sans-serif" fill="#20334e"><text x="40" y="55" font-size="30" font-weight="700">两种通关目标</text><text x="40" y="91" font-size="19">蓝色圆周表示白球边缘，绿色三角形表示目标。</text><rect x="35" y="120" width="540" height="450" rx="15" fill="#edf4ff"/><rect x="625" y="120" width="540" height="450" rx="15" fill="#edf7f2"/><text x="300" y="163" text-anchor="middle" font-size="26" font-weight="700">内切：圆在三角形里面</text><text x="895" y="163" text-anchor="middle" font-size="26" font-weight="700">外接：三角形在圆里面</text><polygon points="300,225 196.077,405 403.923,405" fill="#d8ede4" stroke="#238064" stroke-width="4"/><circle cx="300" cy="345" r="60" fill="white" stroke="#4274c4" stroke-width="4"/><circle cx="895" cy="345" r="120" fill="white" stroke="#4274c4" stroke-width="4"/><polygon points="895,225 791.077,405 998.923,405" fill="#d8ede4" fill-opacity="0.7" stroke="#238064" stroke-width="4"/><text x="300" y="510" text-anchor="middle" font-size="22">圆周同时贴住三条边</text><text x="895" y="510" text-anchor="middle" font-size="22">三个顶点同时落在圆周上</text><text x="600" y="600" text-anchor="middle" font-size="17" fill="#5b6c82">完成任意一种即可通关；只是进入或罩住三角形还不够。</text></g></svg>`;
fs.writeFileSync(path.join(out,'07-两种通关目标.svg'),goalSvg,'utf8');
(async()=>{const root=process.env.CODEX_NODE_MODULES;
 if(root){const sharp=require(path.join(root,'sharp'));for(const file of fs.readdirSync(out).filter(f=>f.endsWith('.svg')))await sharp(path.join(out,file)).png().toFile(path.join(out,file.replace(/\.svg$/,'.png')));}
 console.log('简明流程图与玩法示意图已生成');
})().catch(err=>{console.error(err);process.exitCode=1;});
