//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    

   updateSQL(sqlConn);
    
}
//在这里编写您的函数或者类

public void updateSQL(string sqlConn)
{
    //Console.WriteLine(queryStr );
    //Console.WriteLine(queryItemSql );

    using (MySqlConnection connection = new MySqlConnection(sqlConn))
    {
        connection.Open();
        using (MySqlTransaction tran = connection.BeginTransaction(IsolationLevel.Serializable))
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {            
                //try{
                        cmd.Connection = connection;
                        cmd.Transaction = tran;
                        foreach(string sql in ordersSqlList){
                            cmd.CommandText = sql;
                            int affectedRowCount = cmd.ExecuteNonQuery();
                            Console.WriteLine("affectedRowCount Orders: {0}", affectedRowCount);
                        }
                    
                       foreach(string sql in orderItemsSqlList){
                            cmd.CommandText = sql;
                            int affectedRowCount = cmd.ExecuteNonQuery();
                            Console.WriteLine("affectedRowCount Order Items: {0}", affectedRowCount);
                        }
                       
                        tran.Commit();
                 //   }
               //     catch(Exception e){
               //         tran.Rollback();
            //        } 
            }
        }
        connection.CloseAsync().Wait();
    }
}