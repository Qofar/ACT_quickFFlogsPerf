# ACT_quickFFlogsPerf

### AdvancedCombatTracker :

![preview](preview.png)

### OverlayPlugin / miniparse.html :

```json
{ text: "{DPSPerf}", width: "2em", align: "right", effect: logsTextEffect },

{ text: "{HPSPerf}", width: "2em", align: "right", effect: logsTextEffect },
```

```JavaScript
function logsTextEffect(cell) {
    var value = cell.innerText;
    if (value.startsWith("100")) {
        cell.style.color = "rgb(229,204,128)";
        return;
    }
    if (value.startsWith("99") || value.startsWith("95")) {
        cell.style.color = "rgb(255,128,0)";
        return;
    }
    if (value.startsWith("75")) {
        cell.style.color = "rgb(163,53,238)";
        return;
    }
    if (value.startsWith("50")) {
        cell.style.color = "rgb(0,112,255)";
        return;
    }
    if (value.startsWith("25")) {
        cell.style.color = "rgb(30,255,0)";
        return;
    }
    cell.style.color = "#666";
}
```
