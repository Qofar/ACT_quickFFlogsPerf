// Titania https://www.fflogs.com/zone/statistics/28/#boss=1045&dataset=100&class=Global&spec=Astrologian&dpstype=pdps

const JobSelect = ["Astrologian", "Bard", "BlackMage", "Dancer", "DarkKnight", "Dragoon", "Gunbreaker", "Machinist", "Monk", "Ninja", "Paladin", "RedMage", "Samurai", "Scholar", "Summoner", "Warrior", "WhiteMage"];
const JOB = ["Ast", "Brd", "Blm", "Dnc", "Drk", "Drg", "Gnb", "Mch", "Mnk", "Nin", "Pld", "Rdm", "Sam", "Sch", "Smn", "War", "Whm"];

function copy(str) {
	document.oncopy = function(event) {
		event.clipboardData.setData("text/plain", str);
		event.preventDefault();
	};
	document.execCommand("copy", false, null);
}

chrome.extension.onMessage.addListener(function(request, sender, sendResponse) {
	if (request.command != "fflogsCrawler") {
		return;
	}
	
	let bossname = document.querySelector("span#filter-boss-text").textContent;
	let perf = { "dps": {}, "hps": {} };
	
	let count = 0;
	let timer = setInterval(function () {
		let metric = document.querySelector("span#filter-metric-text").textContent;
		let jobname = JOB[count];
		let dps = [];

		let result = document.body.textContent.match(/series\.data\.push\(([\d\.]+)\)/);
		dps.push(result[1]);
		result = document.body.textContent.match(/series99\.data\.push\(([\d\.]+)\)/);
		dps.push(result[1]);
		result = document.body.textContent.match(/series95\.data\.push\(([\d\.]+)\)/);
		dps.push(result[1]);
		result = document.body.textContent.match(/series75\.data\.push\(([\d\.]+)\)/);
		dps.push(result[1]);
		result = document.body.textContent.match(/series50\.data\.push\(([\d\.]+)\)/);
		dps.push(result[1]);
		result = document.body.textContent.match(/series25\.data\.push\(([\d\.]+)\)/);
		dps.push(result[1]);
		result = document.body.textContent.match(/series10\.data\.push\(([\d\.]+)\)/);
		dps.push(result[1]);

		if (metric == "Damage") {
			perf.dps[jobname] = dps;
		} else {
			perf.hps[jobname] = dps;
		}

		count++;
		if (count >= JobSelect.length) {
			if (metric == "Damage") {
				count = 0;
				document.querySelector("a#class-Global-spec-Astrologian").click();
				document.querySelector("a#metric-hps").click();
			} else {
				clearInterval(timer);

				copy(JSON.stringify(perf));

				sendResponse({ result: "success" });
				alert(bossname + "=> success");
			}
		} else {
			document.querySelector("a#class-Global-spec-" + JobSelect[count]).click();
		}
	}, 2000);

	return true;
});
