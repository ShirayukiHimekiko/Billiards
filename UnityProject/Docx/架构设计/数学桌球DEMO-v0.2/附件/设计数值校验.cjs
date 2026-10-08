// 设计数值自检；不实现运行时连续判定器，不代替 Unity 验收。
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const D = 2.5, k = .8, eps = .08;
const A = [2, 3.2], B = [2-Math.sqrt(1.08),1.4], C = [2+Math.sqrt(1.08),1.4];
const start = [1.0015625,2];
const item = [2.307010480451913,2], itemRadius=.1;
const dist=(a,b)=>Math.hypot(a[0]-b[0],a[1]-b[1]);
const cross=(a,b)=>a[0]*b[1]-a[1]*b[0];
const sub=(a,b)=>[a[0]-b[0],a[1]-b[1]];
const near=(a,b,tol=1e-10)=>assert.ok(Math.abs(a-b)<=tol,`${a} != ${b}`);
const shot=p=>{const r0=.3+2.1*p,v0=3+9*p,a=v0*v0/(2*D);return {p,r0,v0,a,T:2*D/v0,D};};
const travel=(s,t)=>s.v0*t-.5*s.a*t*t;
const at=(s,t)=>[start[0]+travel(s,t),2];
const arrival=s=>(s.v0-Math.sqrt(s.v0*s.v0-2*s.a*(2-start[0])))/s.a;
const geometry=(()=>{const a=dist(B,C),b=dist(C,A),c=dist(A,B),S=Math.abs(cross(sub(B,A),sub(C,A)))/2;
  return {I:[(a*A[0]+b*B[0]+c*C[0])/(a+b+c),(a*A[1]+b*B[1]+c*C[1])/(a+b+c)],rin:2*S/(a+b+c),Rout:a*b*c/(4*S)};
})();
near(geometry.I[0],2);near(geometry.I[1],2);near(geometry.rin,.6);near(geometry.Rout,1.2);
const errors=(pos,r)=>{
  const vertices=[A,B,C];
  const ds=vertices.map((v,i)=>cross(sub(vertices[(i+1)%3],v),sub(pos,v))/dist(vertices[(i+1)%3],v));
  return {inside:ds.every(d=>d>=-1e-10),inscribed:Math.max(...ds.map(d=>Math.abs(d-r))),circumscribed:Math.max(...vertices.map(v=>Math.abs(dist(pos,v)-r)))};
};
const powers=[0,.25,.5,.75,1].map(shot);
for(let i=0;i<powers.length;i++){
  const s=powers[i];near(travel(s,s.T),D);near(s.v0-s.a*s.T,0);
  if(i)assert.ok(s.T<powers[i-1].T && s.r0>powers[i-1].r0);
}
const innerShot=shot(0),innerTime=.375,innerRadius=innerShot.r0+k*innerTime;
near(at(innerShot,innerTime)[0],2);near(innerRadius,.6);
let lo=.25,hi=.42;
for(let i=0;i<100;i++){
  const p=(lo+hi)/2,s=shot(p),t=(1.2-s.r0)/k;
  if(travel(s,t)>2-start[0])lo=p;else hi=p;
}
const outerShot=shot((lo+hi)/2),outerTime=(1.2-outerShot.r0)/k;
near(at(outerShot,outerTime)[0],2);near(outerShot.r0+k*outerTime,1.2);
const toggleShot=shot(.15),targetTime=arrival(toggleShot);
const toggleTime=(targetTime+(.6-toggleShot.r0)/k)/2;
const entryRadius=toggleShot.r0+k*toggleTime;
near(dist(at(toggleShot,toggleTime),item),entryRadius+itemRadius);
assert.ok(dist(start,item)>toggleShot.r0+itemRadius);
// 接触前球在道具左侧且间隙严格递减，确认此处为首次接触。
assert.ok(at(toggleShot,toggleTime)[0]<item[0]);
assert.ok(toggleShot.v0-toggleShot.a*toggleTime+k>0);
const toggleRadius=toggleShot.r0+k*toggleTime-k*(targetTime-toggleTime);
near(toggleRadius,.6);near(at(toggleShot,targetTime)[0],2);
near((toggleShot.r0+k*toggleTime),entryRadius); // 翻转左右极限同半径
const fixtures=[
  {name:'内切基础（无道具）',shot:innerShot,time:innerTime,radius:innerRadius,pos:at(innerShot,innerTime)},
  {name:'外接基础（无道具）',shot:outerShot,time:outerTime,radius:1.2,pos:at(outerShot,outerTime)},
  {name:'默认道具谜题',shot:toggleShot,time:targetTime,radius:toggleRadius,pos:at(toggleShot,targetTime),toggleTime,entryRadius,item,itemRadius}
].map(f=>({...f,errors:errors(f.pos,f.radius)}));
for(const f of fixtures){
  assert.ok(f.time<f.shot.T);assert.ok(f.radius>.1&&f.radius<3);
  assert.ok(f.pos[0]-f.radius>-3&&f.pos[0]+f.radius<7&&f.pos[1]-f.radius>-3&&f.pos[1]+f.radius<7);
  const err=f.name.startsWith('外接')?f.errors.circumscribed:f.errors.inscribed;
  assert.ok(err<1e-10);assert.ok(err<=eps);
}
// 路径 x 单调，半径候选路径均不超过 1.2，因此这些范围约束覆盖到目标的整段。
for(const f of fixtures){
  const maxR=Math.max(f.shot.r0,f.radius,f.entryRadius||0);
  assert.ok(start[0]-maxR>-3 && 2+maxR<7 && 2-maxR>-3 && 2+maxR<7);
}
const report={version:'demo-v0.2',status:'PASS',scope:'公式与精确检查点；非 Unity 运行验收，非最早匹配求根验收',powers,geometry,fixtures};
fs.writeFileSync(path.join(__dirname,'设计数值校验结果.json'),JSON.stringify(report,null,2)+'\n','utf8');
console.log(JSON.stringify({status:report.status,powerSamples:powers.length,fixtureCount:fixtures.length,output:'设计数值校验结果.json'}));
