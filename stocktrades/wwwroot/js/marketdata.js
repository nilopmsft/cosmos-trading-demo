"use strict";

import * as echarts from 'echarts';

var chartDom = document.getElementById('main');
var myChart = echarts.init(chartDom);
var option;
var data = [];

option = {
    xAxis: {
        type: 'time',
        boundaryGap: false
    },
    yAxis: {
        type: 'value'
    },
    series: [
        {
            data: data,
            type: 'line',
            areaStyle: {}
        }
    ],
    visualMap: {
        left: 'right',
        min: 0,
        max: 1,
        inRange: {
            color: ['red', 'green']
        },
        text: ['>0', '<0'],
        calculable: true
    }
};

option && myChart.setOption(option);

var connection = new signalR.HubConnectionBuilder().withUrl("/allOrders").build();

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