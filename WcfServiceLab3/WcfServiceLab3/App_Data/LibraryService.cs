using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Web;
using MySql.Data.MySqlClient;
/// <summary>
/// Сводное описание для LibraryService
/// </summary>
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[WebService(Description = "Library Service", Namespace = XmlNS)]
public class LibraryService : System.Web.Services.WebService
{

    public const string XmlNS = "http://asmx.libraryService.com/";
    private const string databaseConnection = "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=D:\\home\\site\\wwwroot\\App_Data\\library.mdf;Integrated Security=True";
    private static readonly HttpClient client = new HttpClient();

    private const string connStr = "server=remotemysql.com;Port=3306;Database=KkaqCdtZe4;Uid=KkaqCdtZe4;Pwd=oL7JReLSAe";
    private MySqlConnection conn = new MySqlConnection(connStr);

    public LibraryService()
    {
        //Раскомментируйте следующую строку в случае использования сконструированных компонентов 
        //InitializeComponent(); 
    }

    [WebMethod]
    public List<Book> GetAllBooks(string token)
    {
        //"SELECT * FROM BOOK"
        if (!validateToken(token, "GetAllBooks"))
        {
            return null;
        }
        
        return GetBooksParametred(null, null);
    }

    [WebMethod]
    public List<Book> GetAllBooksByName(string token, string name)
    {
        //string queryString = "SELECT * FROM BOOK WHERE name =";
        //queryString += "'" + name + "'";
        if (!validateToken(token, "GetAllBooks"))
        {
            return null;
        }
        return GetBooksParametred(name, null);
    }

    [WebMethod]
    public List<Book> GetAllAvailableBooks(string token)
    {
        //"SELECT * FROM BOOK WHERE isAvailable = 1"
        if (!validateToken(token, "GetAllBooks"))
        {
            return null;
        }
        return GetBooksParametred(null, true);
    }

    private List<Book> GetBooksParametred(String bookName, bool? isAvailable)
    {
        List<Book> bookList = new List<Book>();
        bool isWhere = false;
        //using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            conn.Open();
            string queryString = "SELECT * FROM Book ";
            if (!string.IsNullOrEmpty(bookName))
            {
                queryString += "WHERE name='" + bookName + "'";
                isWhere = true;
            }
            if (isAvailable != null)
            {
                queryString += (!isWhere ? "WHERE" : "AND") + " isAvailable=" + (isAvailable.Value ? "1" : "0");
            }

            MySqlCommand command = new MySqlCommand(queryString, conn);

            MySqlDataReader reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    var book = new Book();

                    book.ID = (int)reader["id"];
                    book.Name = (string)reader["name"];
                    book.AuthorName = (string)reader["author"];
                    book.IsAvailable = (bool)reader["isAvailable"];

                    bookList.Add(book);
                }
            }
            finally
            {
                reader.Close();
                conn.Close();
            }
        }
        return bookList;
    }

    [WebMethod]
    public bool OrderBook(string token, int bookId)
    {
        if (!validateToken(token, "OrderBook"))
        {
            return false;
        }
        return UpdateBookAvailability(bookId, false);
    }

    [WebMethod]
    public bool ReturnBook(string token, int bookId)
    {
        return UpdateBookAvailability(bookId, true);
    }

    private bool UpdateBookAvailability(int bookId, bool isAvailable)
    {

        //using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            conn.Open();
            string queryString = "UPDATE Book SET isAvailable= ";
            queryString += (isAvailable ? "1" : "0") + " ";
            queryString += "WHERE id=" + bookId;

            MySqlCommand command = new MySqlCommand(queryString, conn);
            try
            {
                command.ExecuteNonQuery();
            }
            catch
            {
                return false;
            }
            conn.Close();
            return true;
        }
    }

    [WebMethod]
    public bool AddNewBook(string token, Book book)
    {
        if (!validateToken(token, "AddNewBook"))
        {
            return false;
        }

        //using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            conn.Open();
            string queryString = "INSERT INTO Book VALUES (";
            queryString += book.ID + ", ";
            queryString += book.Name + ", ";
            queryString += book.AuthorName + ", ";
            queryString += (book.IsAvailable ? "1" : "0") + ");";

            MySqlCommand command = new MySqlCommand(queryString, conn);
            try
            {
                command.ExecuteNonQuery();
            }
            catch
            {
                return false;
            }
            conn.Close();
            return true;
        }
    }

    [WebMethod]
    public bool RemoveBook(string token, int bookId)
    {
        if (!validateToken(token, "RemoveBook"))
        {
            return false;
        }

        //using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            conn.Open();
            string queryString = "DELETE FROM Book WHERE id=" + bookId;

            MySqlCommand command = new MySqlCommand(queryString, conn);
            try
            {
                command.ExecuteNonQuery();
            }
            catch
            {
                return false;
            }
            conn.Close();
            return true;
        }
    }

    private enum WebMethodType
    {
        RemoveBook,
        AddNewBook,
        OrderBook,
        ReturnBook,
        GetBooks,
        GetTokens,
        AddToken
    }

    private static List<Token> tokens = new List<Token>();

    private Token findTokenByContents(String tokenContents)
    {
        foreach (Token token in tokens)
        {
            if (token.token.Equals(tokenContents))
            {
                return token;
            }
        }
        return null;
    }

    private bool validateToken(string token, string methodName)
    {
        return true;
        Token referenceToken = findTokenByContents(token);
        if (referenceToken == null ||
            !referenceToken.methodName.Equals(methodName))
        {
            return false;
        }
        DateTime today = DateTime.Today;
        DateTime from = DateTime.ParseExact(referenceToken.paymentInfo.dateFrom, "dd-MM-yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);
        DateTime to = DateTime.ParseExact(referenceToken.paymentInfo.dateTo, "dd-MM-yyyy",
                                   System.Globalization.CultureInfo.InvariantCulture);
        if (today < from || today > to)
        {
            return false;
        }
        return true;
    }

    [WebMethod]
    public List<Token> GetTokens()
    {
        return tokens;
    }

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/AddToken",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
    public Token AddToken(Token token)
    {
        tokens.Add(token);
        return token;
    }
}
