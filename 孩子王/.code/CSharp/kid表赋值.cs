//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    //kid_dt.Rows.Clear();
    新1行row=kid_dt.NewRow();
    
    新1行row["单据号码"]=订单号;
    新1行row["有效日期"]=有效日期带横线;
    新1行row["供应商代号"]=供应商代号;
    新1行row["供应商"]=供应商;
    新1行row["收货仓库代号"]=仓库代号4位;
    新1行row["收货仓库"]=收货仓库;
    新1行row["制单日期"]=制单日期带横线;
    新1行row["制单人"]=制单人;
    新1行row["审核人"]=审核人;
    新1行row["备注"]=备注总;
    新1行row["创建时间"]=当前时间;
    
    新1行row["序号"]=row["序号"].ToString();
    新1行row["商品编码"]=row["商品编码"].ToString();
    新1行row["商品条码"]=row["商品条码"].ToString();
    新1行row["规格"]=row["规格"].ToString();
    新1行row["商品名称"]=row["商品名称"].ToString();
    新1行row["比率"]=row["比率"].ToString();
    新1行row["单位"]=row["单位"].ToString();
    新1行row["整件"]=row["整件"].ToString();
    新1行row["零数"]=row["零数"].ToString();
    新1行row["单品合计"]=row["单品合计"].ToString();
    新1行row["赠品"]=row["赠品"].ToString();
    新1行row["订货单价"]=row["订货单价"].ToString();
    新1行row["订货金额"]=row["订货金额"].ToString();
    
    kid_dt.Rows.Add(新1行row);
}
//在这里编写您的函数或者类