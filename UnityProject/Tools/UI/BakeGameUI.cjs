// 使用项目已有的浏览器烘焙器生成坐标，不执行 UI 验证。
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { chromium } = require('playwright');

async function bake() {
    const converter = path.resolve(__dirname, '../../Assets/TEngine/Extension/HtmlToUGUI/HtmlToJson/HTML 转 JSON 坐标烘焙器.html');
    const html = await fs.readFile(path.join(__dirname, 'LevelSelectUI.html'), 'utf8');
    const options = { headless: true };
    if (process.env.BILLIARDS_CHROME_PATH) options.executablePath = process.env.BILLIARDS_CHROME_PATH;
    const browser = await chromium.launch(options);
    try {
        const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });
        await page.goto(pathToFileURL(converter).href);
        await page.locator('#code-editor').fill(html);
        const json = await page.evaluate(() => {
            bakeAndCopyJSON();
            return document.getElementById('json-output').value;
        });
        await fs.writeFile(path.join(__dirname, 'LevelSelectUI.json'), json, 'utf8');
        process.stdout.write('LevelSelectUI.json 已生成。\n');
    } finally {
        await browser.close();
    }
}

bake().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
