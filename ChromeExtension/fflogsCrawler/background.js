chrome.browserAction.onClicked.addListener(function(tab) {
  chrome.tabs.sendMessage(tab.id, { command: "fflogsCrawler"}, function(response) {
      console.log(response);
    });
});
