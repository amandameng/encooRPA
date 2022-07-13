//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    ordersDataSet =  SplitDataTable(增量订单结果数据表, 10);
    orderItemsDataSet =  SplitDataTable(增量订单详情结果数据表, 10);

}
//在这里编写您的函数或者类

/// <summary>
        /// 分解数据表
        /// </summary>
        /// <param name="originalTab">需要分解的表</param>
        /// <param name="rowsNum">每个表包含的数据量</param>
        /// <returns></returns> 
        public static DataSet SplitDataTable(DataTable originalTab, int rowsNum)
        {

            int tableNum = System.Convert.ToInt32(originalTab.Rows.Count / rowsNum); //相除取整
            int remainder = System.Convert.ToInt32(originalTab.Rows.Count % rowsNum); //相除取余数
            DataSet ds = new DataSet();
            //if one table is big enough to store, use one table
            if (tableNum == 0)
            {
                ds.Tables.Add(originalTab);
            }
            else
            {

                if (remainder > 0) //如果有余数，需要多一张表存余数
                {
                    tableNum++;
                }

                DataTable[] tableSlice = new DataTable[tableNum - 1 + 1];

                //Save orginal columns into new table
                int c = 0;
                for (c = 0; c <= (tableNum - 1); c++)
                {
                    tableSlice[c] = new DataTable();
                    foreach (DataColumn dc in originalTab.Columns)
                    {
                        tableSlice[c].Columns.Add(dc.ColumnName, dc.DataType);
                    }
                }

                //Import Rows
                int i = 0;
                if (remainder > 0)
                {
                    for (i = 0; i <= (tableNum - 1); i++)
                    {
                        //if the current table is not the last table
                        if (i != tableNum - 1)
                        {
                            int j = 0;
                            for (j = i * rowsNum; j <= (((i + 1) * rowsNum) - 1); j++)
                            {
                                tableSlice[i].ImportRow(originalTab.Rows[j]);
                            }
                        }
                        else
                        {
                            int k = 0;
                            //For k = i * rowsNum To (((i + 1) * rowsNum + remainder) - 1)
                            for (k = i * rowsNum; k <= ((i * rowsNum + remainder) - 1); k++)
                            {
                                tableSlice[i].ImportRow(originalTab.Rows[k]);
                            }
                        }
                    }
                }
                else
                {
                    for (i = 0; i <= (tableNum - 1); i++)
                    {
                        int j = 0;
                        for (j = i * rowsNum; j <= (((i + 1) * rowsNum) - 1); j++)
                        {
                            tableSlice[i].ImportRow(originalTab.Rows[j]);
                        }
                    }
                }

                //Add all tables into a dataset
                foreach (DataTable dt in tableSlice)
                {
                    ds.Tables.Add(dt);
                }
            }

            return ds; 
        }