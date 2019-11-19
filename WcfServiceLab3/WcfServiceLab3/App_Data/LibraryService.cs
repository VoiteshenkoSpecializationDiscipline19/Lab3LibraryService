using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Net.Http;
/// <summary>
/// Сводное описание для LibraryService
/// </summary>
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[WebService(Description = "Library Service", Namespace = XmlNS)]
public class LibraryService : System.Web.Services.WebService
{

    public const string XmlNS = "http://asmx.libraryService.com/";
    private const string databaseConnection = "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=D:\\University\\Service-oriented\\Desktop\\WcfServiceLab3\\WcfServiceLab3\\App_Data\\library.mdf;Integrated Security=True";
    private static readonly HttpClient client = new HttpClient();

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
            throw new Exception("not valid token");
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
            throw new Exception("not valid token");
        }
        return GetBooksParametred(name, null);
    }

    [WebMethod]
    public List<Book> GetAllAvailableBooks(string token)
    {
        //"SELECT * FROM BOOK WHERE isAvailable = 1"
        if (!validateToken(token, "GetAllBooks"))
        {
            throw new Exception("not valid token");
        }
        return GetBooksParametred(null, true);
    }

    private List<Book> GetBooksParametred(String bookName, bool? isAvailable)
    {
        List<Book> bookList = new List<Book>();
        bool isWhere = false;
        using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            string queryString = "SELECT * FROM BOOK ";
            if (!string.IsNullOrEmpty(bookName))
            {
                queryString += "WHERE name='" + bookName + "'";
                isWhere = true;
            }
            if (isAvailable != null)
            {
                queryString += (!isWhere ? "WHERE" : "AND") + " isAvailable=" + (isAvailable.Value ? "1" : "0");
            }

            SqlCommand command = new SqlCommand(queryString, connection);
            command.Connection.Open();

            SqlDataReader reader = command.ExecuteReader();
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
            }
        }
        return bookList;
    }

    [WebMethod]
    public bool OrderBook(string token, int bookId)
    {
        if (!validateToken(token, "OrderBook"))
        {
            throw new Exception("not valid token");
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

        using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            string queryString = "UPDATE BOOK SET isAvailable= ";
            queryString += (isAvailable ? "1" : "0") + " ";
            queryString += "WHERE id=" + bookId;

            SqlCommand command = new SqlCommand(queryString, connection);
            try
            {
                command.Connection.Open();
                command.ExecuteNonQuery();
            }
            catch
            {
                return false;
            }
            return true;
        }
    }

    [WebMethod]
    public bool AddNewBook(string token, Book book)
    {
        if (!validateToken(token, "AddNewBook"))
        {
            throw new Exception("not valid token");
        }

        using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            string queryString = "INSERT INTO BOOK VALUES (";
            queryString += book.ID + ", ";
            queryString += book.Name + ", ";
            queryString += book.AuthorName + ", ";
            queryString += (book.IsAvailable ? "1" : "0") + ");";

            SqlCommand command = new SqlCommand(queryString, connection);
            try
            {
                command.Connection.Open();
                command.ExecuteNonQuery();
            }
            catch
            {
                return false;
            }
            return true;
        }
    }

    [WebMethod]
    public bool RemoveBook(string token, int bookId)
    {
        if (!validateToken(token, "RemoveBook"))
        {
            throw new Exception("not valid token");
        }

        using (SqlConnection connection = new SqlConnection(databaseConnection))
        {
            string queryString = "DELETE FROM BOOK WHERE id=" + bookId;

            SqlCommand command = new SqlCommand(queryString, connection);
            try
            {
                command.Connection.Open();
                command.ExecuteNonQuery();
            }
            catch
            {
                return false;
            }
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

    [WebMethod]
    public Token AddToken(Token token)
    {
        tokens.Add(token);
        return token;
    }
}
