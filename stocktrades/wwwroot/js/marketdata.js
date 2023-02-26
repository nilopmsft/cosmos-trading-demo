"use strict";

var chart_children = document.getElementById('charts').children;
var data = {};
var mycharts = {};
for (var i = 0; i < chart_children.length; i++) {
    var symbol = chart_children[i].id.split("-")[1];
    mycharts[symbol] = echarts.init(chart_children[i]);
    data[symbol] = []
    var option;

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
                data: data[symbol],
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

    option && mycharts[symbol].setOption(option);
}



var connection = new signalR.HubConnectionBuilder().withUrl("/stockHub").build();

function onConnected(connection) {
    console.log('connection started');
    connection.on("marketData", function (marketData) { 
        if (marketData.symbol in data) {
            console.log(marketData.symbol);
            data[marketData.symbol].push(
                {
                    name: marketData.timestamp,
                    value:
                        [
                            new Date(marketData.timestamp),
                            (marketData.avgAskPrice + marketData.avgBidPrice) / 2
                        ]
                });           
            mycharts[marketData.symbol].setOption({ series: [{ data: data[marketData.symbol] }] });
        }
    });
}


connection.start()
    .then(() => onConnected(connection))
    .catch(error => console.error(error.message));