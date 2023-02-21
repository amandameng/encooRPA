//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch("343345", @"^\d+$"));
    Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch("SH3345", @"^\d+$"));
    Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch("3345123", @"\d{7}"));
        Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch("SH45123", @"\d{7}"));
}
//在这里编写您的函数或者类