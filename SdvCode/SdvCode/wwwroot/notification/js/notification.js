﻿"use strict"

let notificationConnection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();

notificationConnection.start().then(function () {
    notificationConnection.invoke("GetUserNotificationCount").catch(function (err) {
        return console.error(err.toString());
    });
}).catch(function (err) {
    return console.error(err.toString());
});

notificationConnection.on("ReceiveNotification", function (count) {
    document.getElementById("notificationCount").innerText = count;
})