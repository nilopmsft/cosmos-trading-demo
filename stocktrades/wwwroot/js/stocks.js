$.ajax({
    url: "/api/StockPrices/" + stock_symbol,
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

        var chartDom = document.getElementById('candlestick-chart');
        var myChart = echarts.init(chartDom);
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

        option && myChart.setOption(option);
    }
});