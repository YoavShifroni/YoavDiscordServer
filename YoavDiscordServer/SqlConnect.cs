using MongoDB.Driver.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public class SqlConnect
    {
        /// <summary>
        /// The SQL Connection object - actual connection with the SQL database
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// The name of the database file (assumed to be located in the same directory as the executable).
        /// </summary>
        private const string dbFileName = "Database1.mdf";

        /// <summary>
        /// The base directory of the currently running application (usually the bin\Debug or bin\Release folder).
        /// </summary>
        private string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// The constructor - creating the SQL connection with the database
        /// </summary>
        public SqlConnect()
        {
            string dbFilePath = Path.Combine(baseDirectory, dbFileName);
            string connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True";
            connection = new SqlConnection(connectionString);
        }

        /// <summary>
        /// The function insert the user into the data base
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="email"></param>
        /// <param name="city"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        public int InsertNewUser(string username, string password, string firstName, string lastName, string email,
            string city, string gender, byte[] imageToByteArray)
        {

            string query = "INSERT INTO Users (Username, Password, FirstName, LastName, Email, City, Gender, ProfilePicture) VALUES (@username, @password, @firstname, @lastname, @email, @city, @gender, @imageToByteArray); SELECT SCOPE_IDENTITY();";
            SqlCommand command = new SqlCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@firstname", firstName);
            command.Parameters.AddWithValue("@lastname", lastName);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@city", city);
            command.Parameters.AddWithValue("@gender", gender);
            command.Parameters.AddWithValue("@imageToByteArray", imageToByteArray);
            connection.Open();
            command.Connection = connection;
            int id = Convert.ToInt32(command.ExecuteScalar());
            Console.WriteLine("New Id is: " + id);
            connection.Close();
            return id;
        }

        /// <summary>
        /// The function check if this username exist in the data base
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool IsExist(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            int b = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
            return b > 0;
        }


        /// <summary>
        /// The function return the email of this user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetEmail(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Email FROM Users WHERE Username  = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            string b = (string)command.ExecuteScalar();
            connection.Close();
            return b;
        }


        /// <summary>
        /// The function return the current password of this user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetPassword(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Password FROM Users WHERE Username  = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            string b = (string)command.ExecuteScalar();
            connection.Close();
            return b;
        }


        /// <summary>
        /// The function return the profile picture of the user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public byte[] GetProfilePictureByUsername(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT ProfilePicture FROM Users WHERE Username  = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            byte[] b = (byte[])command.ExecuteScalar();
            connection.Close();
            return b;
        }



        /// <summary>
        /// The function return the profile picture of the user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public byte[] GetProfilePictureByUserId(int userId)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT ProfilePicture FROM Users WHERE Id  = @userId";
            command.Parameters.AddWithValue("@userId", userId);
            connection.Open();
            command.Connection = connection;
            byte[] b = (byte[])command.ExecuteScalar();
            connection.Close();
            return b;
        }

        /// <summary>
        /// The function return the current username of this user id
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetUsernameById(int userId)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Username FROM Users WHERE Id  = @userId";
            command.Parameters.AddWithValue("@userId", userId);
            connection.Open();
            command.Connection = connection;
            string b = (string)command.ExecuteScalar();
            connection.Close();
            return b;
        }

        /// <summary>
        /// The function return the user Id from his username and his password and on the way check if there is a registered
        /// user with this username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int GetUserId(string username, string password)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Id FROM Users WHERE Username = @username AND Password = @password ";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);
            long ticks = DateTime.Now.Ticks;
            connection.Open();
            command.Connection = connection;
            int b = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
            return b;
        }


        /// <summary>
        /// The function update the Password in the database when someone forgot his password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="newPassword"></param>
        public void UpdatePassword(string username, string newPassword)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "UPDATE Users SET Password = @newPassword WHERE Username = @username ";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@newPassword", newPassword);
            connection.Open();
            command.Connection = connection;
            command.ExecuteNonQuery();
            connection.Close();
        }

        /// <summary>
        /// The function update the profile picture in the database when someone wants to update his profile picture
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="userId"></param>
        public void UpdateProfilePictureByUserId(byte[] bytes, int userId)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "UPDATE Users SET ProfilePicture = @newProfilePicture WHERE Id = @id ";
            command.Parameters.AddWithValue("@newProfilePicture", bytes);
            command.Parameters.AddWithValue("@id", userId);
            connection.Open();
            command.Connection = connection;
            command.ExecuteNonQuery();
            connection.Close();
        }

        public List<UserDetails> GetAllUsersDetails()
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Id, Username, ProfilePicture, Role FROM Users";
            connection.Open();
            command.Connection = connection;
            List<UserDetails> details = new List<UserDetails>();
            SqlDataReader reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    details.    Add(new UserDetails((int)reader["Id"], (string)reader["Username"], (byte[])reader["ProfilePicture"], (int)reader["Role"]));
                }
            }
            finally
            {
                // Always call Close when done reading.
                reader.Close();
                connection.Close();
            }
            return details;
        }


        public int GetUserRole(int userId)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Role FROM Users WHERE Id = @userId";
            command.Parameters.AddWithValue("@userId", userId);
            connection.Open();
            command.Connection = connection;
            int b = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
            return b;
        }

        public void UpdateUserRole(int userId, int newRole)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "UPDATE Users SET Role = @newRole WHERE Id = @userId ";
            command.Parameters.AddWithValue("@newRole", newRole);
            command.Parameters.AddWithValue("@userId", userId);
            connection.Open();
            command.Connection = connection;
            command.ExecuteNonQuery();
            connection.Close();
        }

        /// <summary>
        /// Gets a user ID by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User ID, or -1 if not found</returns>
        public int GetUserIdByUsername(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Id FROM Users WHERE Username = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            int b = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
            return b;
        }

        /// <summary>
        /// Inserts a new bot user into the database
        /// </summary>
        /// <param name="username">Bot username</param>
        /// <param name="password">Bot password (hashed)</param>
        /// <param name="firstName">Bot first name (type)</param>
        /// <param name="lastName">Bot last name</param>
        /// <param name="email">Bot email</param>
        /// <param name="city">Bot city</param>
        /// <param name="gender">Bot gender</param>
        /// <param name="profilePicture">Bot profile picture</param>
        /// <param name="role">Bot role (3 = bot)</param>
        /// <returns>The new bot's user ID</returns>
        public int InsertNewBot(string username, string password, string firstName, string lastName, string email,
            string city, string gender, byte[] profilePicture, int role)
        {
            string hashedPassword = CommandHandlerForSingleUser.CreateSha256(password);
            string query = "INSERT INTO Users (Username, Password, FirstName, LastName, Email, City, Gender, ProfilePicture, Role) VALUES (@username, @password, @firstname, @lastname, @email, @city, @gender, @profilePicture, @role)";
            SqlCommand command = new SqlCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", hashedPassword);
            command.Parameters.AddWithValue("@firstname", firstName);
            command.Parameters.AddWithValue("@lastname", lastName);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@city", city);
            command.Parameters.AddWithValue("@gender", gender);
            command.Parameters.AddWithValue("@profilePicture", profilePicture);
            command.Parameters.AddWithValue("@role", role);
            connection.Open();
            command.Connection = connection;
            int id = command.ExecuteNonQuery();
            Console.WriteLine("New Bot Id is: " + id);
            connection.Close();
            return id;
        }
    }

}

