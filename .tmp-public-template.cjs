const fs=require('fs');
for (const name of ['login','register']) {
 const file=`Web/SmartMicrogrid.Web/${name}.html`;
 let s=fs.readFileSync(file,'utf8');
 s=s.replace(/<div class="auth-visual-brand">[\s\S]*?<\/h1>\s*<\/div>/, '<span class="auth-visual-eyebrow">SOLAR ENERGY. SHARED POSSIBILITIES.</span>');
 s=s.replace(/(<div class="auth-header">\s*)<h2>(.*?)<\/h2>/,'$1<h1>$2</h1>');
 if(name==='register') {
   s=s.replace(/<\/div>\s*<\/div>\s*<!-- Form Side -->/,'</div>\n\n      <!-- Form Side -->');
   s=s.replace('id="hero-title">Prosumer Registration','id="hero-title">Power a connected community');
 }
 fs.writeFileSync(file,s);
}
const file='Web/SmartMicrogrid.Web/mobile-redirect.html';
let s=fs.readFileSync(file,'utf8').replace('<h2>Mobile App Required</h2>','<h1>Continue on mobile</h1>');
// Clear the old session before rendering public navigation, so it cannot expose dead logout controls.
s=s.replace('    renderNavbar();','').replace('    SessionManager.clearSession();','    SessionManager.clearSession();\n    renderNavbar();');
fs.writeFileSync(file,s);
