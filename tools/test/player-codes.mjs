// Browser test of player codes and saves, in headless Chromium.
//
//   cd tools/test && npm install && npx playwright install chromium
//   node player-codes.mjs [game URL] [screenshot folder]
//
// URL: a local build (python3 -m http.server 8931 --directory build/WebGL) or a Vault test build. It gets a new
// player code, starts a New Game and checks the save reached the player code service; continues with that code in a
// fresh browser; and checks an unknown code is refused. It creates a real player under BEATS_EMPIRE each run.
import { chromium } from 'playwright';
import zlib from 'node:zlib';
const [url = 'http://127.0.0.1:8931/', out = '.'] = process.argv.slice(2);
const API = 'https://fieldday-web.wcer.wisc.edu/wsgi-bin/opengamedata.wsgi/player/';
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'] });
const errs = [];
async function computer(tag) {
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });   // fresh storage = another computer
  const page = await ctx.newPage();
  page.on('console', m => { const t = m.text(); if (/exception|error/i.test(t) || t.startsWith('[save]')) errs.push(`${tag}: ${t.split('\n')[0]}`); });
  page.on('pageerror', e => errs.push(`${tag} pageerror ${e.message}`));
  await page.goto(url);
  return { ctx, page };
}
const serverState = async (code) => {
  const r = await (await fetch(`${API}${code}/game/BEATS_EMPIRE/state`)).json();
  const s = r.val && r.val[0];
  if (!s || !s.startsWith('BE1:')) return { raw: s && s.slice(0, 40) };
  return { packed: s.length, record: JSON.parse(zlib.gunzipSync(Buffer.from(s.slice(4), 'base64')).toString()) };
};

// 1. New player on computer A
let { ctx, page } = await computer('A');
await page.screenshot({ path: `${out}/A1-code-panel.png` });
await page.click('#new-player-button');
await page.waitForSelector('#play-prompt', { state: 'visible', timeout: 30000 });
const code = await page.$eval('#code-banner b', b => b.textContent);
console.log('new code:', code);
await page.waitForSelector('#play-button:not([disabled])', { timeout: 600000 });
await page.screenshot({ path: `${out}/A2-play.png` });
await page.click('#play-button');
await page.waitForTimeout(15000);
await page.mouse.click(636, 571);            // NEW GAME (no save yet, so it's the top button)
await page.waitForTimeout(25000);
await page.screenshot({ path: `${out}/A3-game.png` });
await page.waitForFunction(() => window.BeatsSaves.pending() === 0, null, { timeout: 60000 });
const local = await page.evaluate(c => JSON.parse(localStorage.getItem('beats-empire/save/' + c)), code);
const srv = await serverState(code);
console.log('A local save:', local ? `${local.save.length} chars` : 'none',
  '| server:', srv.record ? `${srv.packed} chars packed, same as local: ${srv.record.savedAt === local.savedAt && srv.record.save === local.save}` : JSON.stringify(srv));
await ctx.close();

// 2. Computer B continues with the code
({ ctx, page } = await computer('B'));
await page.fill('#code-input', code);
await page.click('#continue-button');
await page.waitForSelector('#play-prompt', { state: 'visible', timeout: 30000 });
console.log('B banner:', await page.$eval('#code-banner', b => b.textContent));
await page.waitForSelector('#play-button:not([disabled])', { timeout: 600000 });
await page.click('#play-button');
await page.waitForTimeout(15000);
await page.screenshot({ path: `${out}/B1-title.png` });
await page.mouse.click(652, 558);            // CONTINUE
await page.waitForTimeout(25000);
await page.screenshot({ path: `${out}/B2-continued.png` });
await ctx.close();

// 3. Unknown code
({ ctx, page } = await computer('C'));
await page.fill('#code-input', 'NoSuchPlayerZz9');
await page.click('#continue-button');
await page.waitForFunction(() => /No Beats Empire game/.test(document.getElementById('code-message').textContent), null, { timeout: 30000 });
console.log('C message:', await page.$eval('#code-message', m => m.textContent));
await ctx.close();

console.log('errors/log:\n' + (errs.length ? errs.slice(0, 20).join('\n') : 'none'));
await browser.close();
