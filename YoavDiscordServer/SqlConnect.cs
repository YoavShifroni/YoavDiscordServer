﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        /// The constructor - creating the SQL connection with the database
        /// </summary>
        public SqlConnect()
        {

            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Visual Studio\YoavDiscordServer\YoavDiscordServer\Database1.mdf;Integrated Security=True";
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

            string query = "INSERT INTO Users (Username, Password, FirstName, LastName, Email, City, Gender, ProfilePicture) VALUES (@username, @password, @firstname, @lastname, @email, @city, @gender, @imageToByteArray)";
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
            int id = command.ExecuteNonQuery();
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
                    details.Add(new UserDetails((int)reader["Id"], (string)reader["Username"], (byte[])reader["ProfilePicture"], (int)reader["Role"]));
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

    }
}
