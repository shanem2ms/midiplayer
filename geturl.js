function sleep(ms) {
      return new Promise(resolve => setTimeout(resolve, ms));
   }

function asyncGet(url) {
    return new Promise(function (resolve, reject) {
        let xhr = new XMLHttpRequest();
        xhr.open("GET", url);
        xhr.onload = function () {
            if (this.status >= 200 && this.status < 300) {
                resolve(xhr.response);
            } else {
                reject({
                    status: this.status,
                    statusText: xhr.statusText
                });
            }
        };
        xhr.onerror = function () {
            reject({
                status: this.status,
                statusText: xhr.statusText
            });
        };
        xhr.send();
    });
}

async function GetUrls() 
{
console.clear();

const batch = 50;
let sleeptime = 16;
var urls = {};
var midsize = mids.length;
console.log(midsize);
var nextdump = 1000;
for (let y = 0; y < midsize; y+= batch) 
{
    console.log(`getting ${y} to ${y+batch}`);
    for (let x = y; x < y + batch; x++)
    {
        if (x >= midsize)
            break;
        let retry = true;
        while (retry)
        {
            let response = "";
            try {
                var url = `https://bitmidi.com${mids[x]}`;
                response = await asyncGet(url);
                var doc = document.createElement("html");
                doc.innerHTML = response;                
                var main = document.evaluate("//main/div[1]/div[4]/div[2]/p/a/@href", doc, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.value;
                urls[mids[x]] = main;
                retry = false;
                sleeptime = 16;
            } 
            catch (error) {
                console.log(error);
                console.log(`sleep for ${sleeptime}s`)
                await sleep(sleeptime*1000);
                sleeptime *= 2;
                retry = true;
            }
        }
    }
    if (y > nextdump)
    {
        console.log(urls);
        urls = {};
        nextdump = y + 1000;
    }
}
console.log(urls);
}

function ReadMids()
{
    mids = JSON.parse(`[  
    "/dash-berlin-waiting-mid",
    "/a-string-of-pearls-1-mid",
    "/yomanda-synth-and-strings-mid",
    "/blink182-please-take-me-home-k-mid",
    "/nightwish-10th-man-down-mid",
    "/rollercoaster-mid",
    "/coldplay-every_teardrop_is_a_waterfall-mid"]`);    
}


ReadMids();
GetUrls();