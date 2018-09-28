"use strict";

//If you want to only use websockets
//var transport = signalR.TransportType.LongPolling;

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/messageHub")
    //.transport(transport)
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveMessage", function (user, message) {    
    var li = document.createElement("li");      
    var anchor = document.createElement("a");    

    anchor.href = message;
    anchor.text = message;     

    li.appendChild(anchor);
    document.getElementById("messageContainer").appendChild(li);
});

connection.on("ShowProgress", function (progress) {
    //console.log("Upload " + progress + "% complete!");
    var progressView = document.getElementById("progressView");
    progressView.textContent = "Incoming file: " + progress + " %";
});

connection.on("broadcastMessage", function (user, message) {
    console.log(user + ': ' + message);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});