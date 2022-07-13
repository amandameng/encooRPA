//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(增量订单结果数据表==null){
        增量订单结果数据表 = orderDT;
    }else{
        增量订单结果数据表.Merge(orderDT, true,MissingSchemaAction.Add);
    }
}
//在这里编写您的函数或者类