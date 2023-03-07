"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/stockHub").build();

function onConnected(connection) {
    console.log('connection started');
    connection.on("executedOrder", function (orderJson) {
        var order = JSON.parse(orderJson);
        var li = document.createElement("li");
        orderList = document.getElementById("ordersList");
        orderList.prepend(li);
        // We can assign user-supplied strings to an element's textContent because it
        // is not interpreted as markup. If you're assigning in any other way, you
        // should be aware of possible script injection concerns.

        var created = new Date(order.createdAt);
        var createdString = created.toLocaleString('en-US', { timeZone: 'UTC' });

        li.textContent = `${createdString}: ${order.action} order by ${order.customerId} of ${order.symbol} @ \$${order.price}`;

        if (orderList.children.length > 200) {
            orderList.removeChild(orderList.lastChild);
        }
    });
}


connection.start()
    .then(() => onConnected(connection))
    .catch(error => console.error(error.message));