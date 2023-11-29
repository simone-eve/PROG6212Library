using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace PROG6212Library
{
    public class POECalculation
    {
        public SqlConnection sqlConnection;
        
        public int SelfStudyCalculation(int numberCredits, int semesterWeek, int weeklyHours)
        {
            int selfStudyHours = (((numberCredits * 10) / semesterWeek) - weeklyHours);
            return selfStudyHours;
        }

        //calculation to work out how many self study hours are remaining after the user has tracked hours 
        public int RemainingSelfStudyCalculation(DateTime date, string moduleNames, int hours)
        {
            DateTime firstDay = date.AddDays(-(int)date.DayOfWeek);
            DateTime endDay = firstDay.AddDays(6);
            int remainingHours = 0;

            using (SqlConnection connection = new SqlConnection("Data Source=lab000000\\SQLEXPRESS;Initial Catalog=PROGPOE;Integrated Security=True"))
            {
                connection.Open();

                // Retrieve study records for the selected week
                string selectStudyRecordsQuery = "SELECT moduleName, studyHours FROM SelfStudy_Tracker " +
                                                 "WHERE currentDate >= @StartDate AND currentDate <= @EndDate";

                using (SqlCommand command = new SqlCommand(selectStudyRecordsQuery, connection))
                {
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = firstDay;
                    command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDay;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Calculate total study hours for the selected week
                        var totalStudyHours = new Dictionary<string, int>();
                        while (reader.Read())
                        {
                            string moduleName = reader["moduleName"].ToString();
                            int studyHours = Convert.ToInt32(reader["studyHours"]);

                            if (totalStudyHours.ContainsKey(moduleName))
                            {
                                totalStudyHours[moduleName] += studyHours;
                            }
                            else
                            {
                                totalStudyHours[moduleName] = studyHours;
                            }
                        }

                        // Close the outer SqlDataReader before executing the inner one
                        reader.Close();

                        // Retrieve the module from the Modules table
                        string selectModuleQuery = "SELECT module_name, self_study_hours FROM module WHERE module_name = @ModuleName";
                        using (SqlCommand moduleCommand = new SqlCommand(selectModuleQuery, connection))
                        {
                            moduleCommand.Parameters.Add("@ModuleName", SqlDbType.VarChar).Value = moduleNames;

                            using (SqlDataReader moduleReader = moduleCommand.ExecuteReader())
                            {
                                if (moduleReader.Read())
                                {
                                    int initialSelfStudyHours = Convert.ToInt32(moduleReader["self_study_hours"]);
                                    remainingHours = initialSelfStudyHours - (totalStudyHours.ContainsKey(moduleNames) ? totalStudyHours[moduleNames] : 0);

                                    // Update the module in the Modules table
                                    string updateModuleQuery = "UPDATE module SET self_study_hours = @SelfStudyHours WHERE module_name = @ModuleName";
                                    using (SqlCommand updateCommand = new SqlCommand(updateModuleQuery, connection))
                                    {
                                        updateCommand.Parameters.Add("@SelfStudyHours", SqlDbType.Int).Value = remainingHours;
                                        updateCommand.Parameters.Add("@ModuleName", SqlDbType.VarChar).Value = moduleNames;
                                        updateCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return remainingHours;


            //return remainingHours; // If you need to return the remaining hours, uncomment this line and adjust the return type accordingly




        }

        public bool DoesUserHaveSemesterInfo(int userId)
        {
            using (SqlConnection connection = new SqlConnection(Connection.Conn))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM semester WHERE users_id = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Set the parameter for the user ID.
                    command.Parameters.Add(new SqlParameter("@UserID", userId));

                    int count = (int)command.ExecuteScalar();

                    return count > 0;
                }
            }
        }

        public int GetSemesterWeeks(int User_Id) //method to fetch the amount of semester weeks from the database
        {
            using (SqlConnection connection = new SqlConnection(Connection.Conn))
            {
                connection.Open();

                string query = "SELECT semester_weeks FROM semester WHERE users_id = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", User_Id));

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) //error handling to ensure the application does not crash if there is no semester info in the db
                        {
                            return (int)reader["semester_weeks"];
                        }
                        else
                        {

                            throw new Exception("No semester information found for the user.");
                        }
                    }
                }
            }
        }

    }
}