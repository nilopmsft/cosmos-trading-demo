var chartDom = document.getElementById('candlestick-chart');
var myChart = echarts.init(chartDom);
myChart.showLoading();

$.ajax({
    url: "/api/Stock/" + stock_symbol,
    success: function (result) {
        prices = result;

        var dates = [];
        var price_history = []

        $.each(prices, function (index, price) {
            var date_hold_array = price.id.split("-");
            date_hold_array.pop();
            dates.push(date_hold_array.join("-"));
            price_history.push([price.closePrice, price.openPrice, price.minAskPrice, price.maxBidPrice]);
        });

        var option;

        option = {
            xAxis: {
                data: dates
            },
            yAxis: {},
            series: [
                {
                    type: 'candlestick',
                    data: price_history
                }
            ]
        };
        myChart.hideLoading();
        option && myChart.setOption(option);
    }
});