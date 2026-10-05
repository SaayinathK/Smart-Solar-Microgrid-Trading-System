const fs=require('fs'),path=require('path');
const root='Android/SmartMicrogrid.Android/app/src/main';
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]);}
function edit(file,fn){fs.writeFileSync(file,fn(fs.readFileSync(file,'utf8')));}
for(const file of walk(root+'/java').filter(f=>f.endsWith('.kt')&&!f.endsWith('WorkspaceActivity.kt'))){
  const s=fs.readFileSync(file,'utf8');
  if (!/class \w+\s*:\s*AppCompatActivity\(\)/.test(s)) continue;
  edit(file,s=>s.replace('import androidx.appcompat.app.AppCompatActivity','import com.smartmicrogrid.ui.WorkspaceActivity').replace(/(class \w+\s*:\s*)AppCompatActivity\(\)/,'$1WorkspaceActivity()'));
}
edit(root+'/res/values/strings.xml',s=>s.replace('</resources>','    <string name="workspace_brand">SmartMicrogrid</string>\n    <string name="workspace_footer">Connected energy communities</string>\n</resources>'));

// Remove the separate home greeting/brand row and use its live name in the hero.
function removeBlock(s,start,tag){
 const re=new RegExp(`<\\/?${tag}\\b[^>]*>`,'g');re.lastIndex=start;let depth=0,m;
 while((m=re.exec(s))){depth+=m[0].startsWith('</')?-1:1;if(depth===0)return s.slice(0,start)+s.slice(re.lastIndex);}
 throw new Error('Unbalanced block '+tag);
}
edit(root+'/res/layout/fragment_home.xml',s=>{
 const start=s.indexOf('        <LinearLayout',s.indexOf('android:padding="@dimen/screen_gutter"'));
 s=removeBlock(s,start,'LinearLayout');
 return s.replace('android:text="@string/solar_home_headline"','android:id="@+id/tv_greeting_name"\n                        android:text="@string/solar_welcome"')
   .replace('android:minHeight="220dp"','android:minHeight="160dp"')
   .replace('android:textSize="28sp"','android:textSize="@dimen/screen_title_size"')
   .replace('android:layout_marginTop="20dp"','android:layout_marginTop="0dp"');
});
edit(root+'/res/layout/fragment_admin_dashboard.xml',s=>{
 const start=s.indexOf('        <LinearLayout',s.indexOf('android:padding="@dimen/screen_gutter"'));
 s=removeBlock(s,start,'LinearLayout');
 const title=`<TextView
                        android:id="@+id/m4_dashboard_title"
                        android:layout_width="match_parent"
                        android:layout_height="wrap_content"
                        android:layout_marginTop="8dp"
                        android:text="@string/m4_portal_title"
                        android:textColor="#FFFFFF"
                        android:textSize="@dimen/screen_title_size"
                        android:fontFamily="sans-serif-medium" />

                    `;
 return s.replace('<TextView\r\n                        android:id="@+id/m4_dashboard_caption"',title+'<TextView\r\n                        android:id="@+id/m4_dashboard_caption"')
 .replace('android:textSize="18sp"','android:textSize="14sp"').replace('android:layout_marginTop="16dp"','android:layout_marginTop="0dp"');
});
// Keep data legible: energy is the primary value, references and dates secondary.
edit(root+'/res/layout/item_transaction.xml',s=>s.replace('app:cardElevation="4dp"','app:cardElevation="1dp"').replace('app:strokeColor="#332563EB"','app:strokeColor="@color/surface_outline"').replace('app:cardCornerRadius="16dp"','app:cardCornerRadius="@dimen/surface_corner"').replace(/(<TextView\s+android:id="@\+id\/tv_amount"[\s\S]*?)android:textSize="14sp"/,'$1android:fontFamily="sans-serif-medium"\n            android:textSize="22sp"'));
for(const file of ['fragment_my_transactions','fragment_transaction_list','fragment_transaction_history']){
 edit(root+'/res/layout/'+file+'.xml',s=>s.replace('android:src="@drawable/img_solar_farm"','android:src="@drawable/img_battery_storage"').replace('android:paddingBottom="80dp"','android:paddingBottom="20dp"'));
}
for(const file of ['item_reservation','item_energy_slot','item_m2_energy_slot','item_approved_reservation']){
 edit(root+'/res/layout/'+file+'.xml',s=>s.replace(/app:cardElevation="[4-8]dp"/g,'app:cardElevation="1dp"').replace(/app:cardCornerRadius="(?:14|16|20|24)dp"/g,'app:cardCornerRadius="@dimen/surface_corner"'));
}
