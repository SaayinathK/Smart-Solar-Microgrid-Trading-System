const { chromium } = require('C:/Users/User/AppData/Local/npm-cache/_npx/1196d833f288caa4/node_modules/playwright');
const fs=require('fs'),path=require('path'),http=require('http'),assert=require('node:assert/strict');
const root=path.resolve('Web/SmartMicrogrid.Web');
const walk=d=>fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(d,e.name)):[path.join(d,e.name)]);
const server=http.createServer((req,res)=>{
 const file=path.resolve(root,'.'+new URL(req.url,'http://localhost').pathname);
 if(!file.startsWith(root+path.sep)) return res.writeHead(403).end();
 fs.readFile(file,(e,b)=>{res.writeHead(e?404:200,{'Content-Type':({'.html':'text/html','.css':'text/css','.js':'text/javascript','.png':'image/png','.jpg':'image/jpeg'})[path.extname(file)]||'text/plain'});res.end(e?'missing':b);});
});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));
 const base=`http://127.0.0.1:${server.address().port}`;
 const browser=await chromium.launch({headless:true,executablePath:'C:/Users/User/AppData/Local/ms-playwright/chromium-1217/chrome-win64/chrome.exe'});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1000},reducedMotion:'reduce'});
  const errors=[],issues=[];page.on('pageerror',e=>errors.push(e.message));
  let populated=false,txRequests=0;
  const tx=[{id:'SMG-2041',reservationId:'RES-1024',prosumerId:'PRO-008',energyAmount:48,status:'Completed',createdAt:'2026-10-05T08:30:00Z',verificationTime:'2026-10-05T09:00:00Z',energyTransferTime:'2026-10-05T09:15:00Z'},{id:'SMG-2042',reservationId:'RES-1025',prosumerId:'PRO-012',energyAmount:25,status:'Pending',createdAt:'2026-10-05T10:30:00Z'}];
  await page.route('**/*',r=>{
   const url=new URL(r.request().url());
   if(url.port==='5050'){
    if(populated&&url.pathname.endsWith('/transactions')){txRequests++;return r.fulfill({contentType:'application/json',body:JSON.stringify({success:true,data:tx})});}
    return r.fulfill({status:503,contentType:'application/json',body:JSON.stringify({success:false,message:'Preview: service unavailable'})});
   }
   if(r.request().url().startsWith(base)||url.hostname==='cdn.jsdelivr.net'||url.hostname==='fonts.googleapis.com'||url.hostname==='fonts.gstatic.com')return r.continue();
   return r.abort();
  });
  await page.goto(base+'/index.html');
  const routes=['/dashboard.html',...walk(root+'/pages').filter(f=>f.endsWith('.html')&&!f.endsWith(path.join('M1','dashboard.html'))).map(f=>'/'+path.relative(root,f).replaceAll('\\','/'))];
  for(const route of routes){
   await page.evaluate(role=>{localStorage.setItem('smart_microgrid_token','preview');localStorage.setItem('smart_microgrid_user',JSON.stringify({role,firstName:'Alex',lastName:'Perera'}));},route.includes('/M3/')?'MicrogridOperator':'Admin');
   await page.goto(base+route+'?id=preview',{waitUntil:'load'});
   const source=fs.readFileSync(path.join(root,route),'utf8');
   const structure=await page.evaluate(html=>{
    const original=new DOMParser().parseFromString(html,'text/html').querySelector('[data-page-intro]');
    const ids=[...(original?.querySelectorAll('[id]')||[])].map(e=>e.id);
    return {headers:document.querySelectorAll('.app-header').length,intros:document.querySelectorAll('.page-intro').length,footers:document.querySelectorAll('footer.footer').length,titles:document.querySelectorAll('main h1').length,missingIds:ids.filter(id=>!document.getElementById(id)),imageReady:!!document.querySelector('.page-intro-photo img')?.naturalWidth};
   },source);
   if(structure.headers!==1||structure.intros!==1||structure.footers!==1||structure.titles!==1||structure.missingIds.length||!structure.imageReady)issues.push({route,...structure});
   for(const width of [390,1440]){
    await page.setViewportSize({width,height:1000});
    if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1))issues.push({route,width,overflow:await page.evaluate(()=>document.documentElement.scrollWidth)});
   }
  }
  populated=true;
  await page.evaluate(()=>localStorage.setItem('smart_microgrid_theme','dark'));
  await page.goto(base+'/pages/M3/transactions.html');
  await page.waitForFunction(()=>document.querySelectorAll('#transactions-table-body tr').length===2);
  assert.equal(await page.getByRole('heading',{name:'Energy Transactions',exact:true}).count(),1);
  assert.equal(await page.locator('.hero-image-banner').count(),0);
  await page.locator('#transaction-search').fill('2041');
  await page.waitForFunction(()=>document.querySelectorAll('#transactions-table-body tr').length===1);
  await page.locator('#transaction-search').fill('');
  await page.locator('#refresh-transactions').click();
  await page.waitForFunction(()=>document.querySelectorAll('#transactions-table-body tr').length===2);
  assert.equal(txRequests,2);
  fs.mkdirSync('.ui-review',{recursive:true});
  await page.screenshot({path:'.ui-review/transactions-template.png',fullPage:true,animations:'disabled'});
  await page.setViewportSize({width:390,height:844});
  await page.screenshot({path:'.ui-review/transactions-mobile-template.png',fullPage:true,animations:'disabled'});
  await page.goto(base+'/pages/M3/transactions.html?view=history');
  assert.equal(await page.getByRole('heading',{name:'Transaction History',exact:true}).count(),1);
  assert.equal(await page.locator('#transactions-list-title').textContent(),'Past transfers');
  await page.evaluate(()=>localStorage.clear());
  for(const file of ['index.html','login.html','register.html','mobile-redirect.html']){
   await page.goto(base+'/'+file);
   for(const width of [390,1440]){
    await page.setViewportSize({width,height:1000});
    if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1))issues.push({route:file,width,overflow:true});
   }
   assert.equal(await page.locator('footer.footer').count(),1);
   if(file==='register.html')assert.equal(await page.locator('.auth-container > .auth-card').count(),1);
  }
  console.log(JSON.stringify({pages:routes.length,issues,errors},null,2));
  assert.deepEqual(issues,[]);assert.deepEqual(errors,[]);
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
