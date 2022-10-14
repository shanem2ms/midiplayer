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
var urls = [];

const batch = 50;
let sleeptime = 16;
let totalpages = 7500;
for (let y = 585; y < 1000; y+= batch) 
{
    console.log(`getting ${y} to ${y+batch}`);
    for (let x = y; x < y + batch; x++)
    {
        let retry = true;
        while (retry)
        {
            let response = "";
            try {
                response = await asyncGet(`https://bitmidi.com/?page=${x}`);
                var doc = document.createElement("html");
                doc.innerHTML = response;
                var main = document.evaluate("//main/div[1]/div[2]", doc, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                var links = main.getElementsByTagName("a");
                
                for (var i=0; i<links.length; i++) {
                    urls.push(links[i].getAttribute("href"));
                }
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
    await sleep(5000);
}
console.log(urls);
}

GetUrls();