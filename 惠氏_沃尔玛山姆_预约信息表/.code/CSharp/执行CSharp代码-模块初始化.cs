//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    isSam = (bool)dtRow_ModuleSettings["isSam"];

    int days = 30;
    指定日期 = DateTime.Parse(dtRow_ProjectSettings["指定日期"].ToString()); // 默认是当天时间
    string maxDateStr = 指定日期.ToString("yyyy-MM-dd");

    string prev30DayStr = 指定日期.AddDays(-days).ToString("yyyy-MM-dd");
    // 因为tracker表里面存的 order_capture_date 时间使用的UTC时间，所以查询当前的订单的时间条件是大于等于昨天，查询后需要遍历数据表，筛选出今天的订单。
    successOrdersTrackerSQL = string.Format(@"SELECT *
                                                        				FROM tracker 
                                                        				where order_capture_date >= '{0}' and (ship_to_code in (select ship_to from ship_to_sold_to where customer_name = '{1}') or customer_name like '{1}%')
                                                        				and isSuccess = '成功' and POID is not null and POID !=''", 指定日期.AddDays(-1).ToString("yyyy-MM-dd"), curCustomerName);

    string orderByCaptureDaySql  = string.Format(@"select count(1) countByCaptureDate, order_number from (
                                                             select count(1) count, order_number, date_format(created_time, '%Y-%m-%d') from walmart_orders where date_format(created_time, '%Y-%m-%d') >= '{0}' and date_format(created_time, '%Y-%m-%d')  <= '{1}' and customer_name = '{2}'
                                                             group by order_number, date_format(created_time, '%Y-%m-%d')) s1 group by order_number", prev30DayStr, maxDateStr, curCustomerName);
    
   // 上面查询结果 countByCaptureDate 大于1 则下面的recent30DaysLatestOrdersSQL 取 min， 等于1取max，等于1说明一天抓取了这一单包括了改单的部分。分出两组订单号，下面分别查询出结果再合并在一起。

    // 下面查询语句查询了沃尔玛和山姆两个平台的原始订单，是因为现在山姆门店订单在采集的时候是在沃尔玛模块流程中处理的，客户名为沃尔玛，实际查询山姆订单的时候会出现搜索不到的情况
    recent30DaysLatestOrdersSQL = string.Format(@"select * from walmart_orders join
                                                                        (select  max(create_date_time) max_create_date_time, order_number, customer_name from walmart_orders where date_format(created_time, '%Y-%m-%d') >= '{0}' and 
                                                                        date_format(created_time, '%Y-%m-%d')  <= '{1}' and customer_name in ('沃尔玛', '山姆') group by order_number, customer_name) s2 
                                                                        on s2.max_create_date_time = walmart_orders.create_date_time and s2.order_number = walmart_orders.order_number where walmart_orders.customer_name in ('沃尔玛', '山姆')", prev30DayStr, maxDateStr);
}
//在这里编写您的函数或者类