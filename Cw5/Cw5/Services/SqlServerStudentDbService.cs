using Cw5.DTO.Requests;
using Cw5.DTO.Responses;
using Cw5.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace Cw5.Services
{
    public class SqlServerStudentDbService : IStudentDbService
    {
        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request)
        {
            using (var conect = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var comend = new SqlCommand())
            {
                var response = new EnrollStudentResponse();
                comend.Connection = conect;
                conect.Open();
                var tran = conect.BeginTransaction();
                comend.Transaction = tran;
                comend.CommandText = "SELECT IdStudy FROM studies WHERE name=@name";
                comend.Parameters.AddWithValue("name", request.Studies);
                var dr = comend.ExecuteReader();
                if (!dr.Read())
                {
                    dr.Close();
                    tran.Rollback();
                    throw new ArgumentException("Studia " + request.Studies + " nie isnieją");
                }
                int idstudies = (int)dr["IdStudy"];
                response.IdStudies = idstudies;
                response.Semester = 1;
                dr.Close();
                comend.Parameters.Clear();
                comend.CommandText = "SELECT TOP 1 IdEnrollment, StartDate FROM enrollment WHERE semester = 1 AND IdStudy = @idStudy order by StartDate desc";
                comend.Parameters.AddWithValue("idStudy", idstudies);
                dr = comend.ExecuteReader();
                if (!dr.Read())
                {
                    dr.Close();
                    comend.CommandText = "INSERT INTO ENROLLMENT(IdEnrollment,Semester,IdStudy,StartDate) OUTPUT INSERTED.IdEnrollment VALUES((SELECT MAX(E.IdEnrollment) FROM Enrollment E) + 1,1,@idStudy,@startDate";
                    var studiesStartDate = DateTime.Now;
                    comend.Parameters.AddWithValue("startDate", studiesStartDate);
                    dr = comend.ExecuteReader();
                    dr.Read();
                    response.IdEnrollment = (int)dr["IdEnrollment"];
                    response.StartDate = studiesStartDate;
                }
                else
                {
                    response.IdEnrollment = (int)dr["IdEnrollment"];
                    response.StartDate = (DateTime)dr["StartDate"];
                }
                dr.Close();
                comend.Parameters.Clear();
                comend.CommandText = "INSERT INTO StudentAPBD(IndexNumber,FirstName,LastName,BirthDate,IdEnrollment) VALUES(@index,@fname,@lname,@bdate,@idenrollment)";
                comend.Parameters.AddWithValue("index", request.IndexNumber);
                comend.Parameters.AddWithValue("fname", request.FirstName);
                comend.Parameters.AddWithValue("lname", request.LastName);
                comend.Parameters.AddWithValue("bdate", request.BirthDate);
                comend.Parameters.AddWithValue("idenrollment", response.IdEnrollment);
                try
                {
                    dr = comend.ExecuteReader();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    dr.Close();
                    tran.Rollback();
                    throw new ArgumentException("Duplikat numeru indeksu");
                }
                response.IndexNumber = request.IndexNumber;
                dr.Close();
                tran.Commit();
                return response;
            }
        }

        public PromoteStudentsResponse PromoteStudents(PromoteStudentsRequest request)
        {
            using (var conect = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var comend = new SqlCommand())
            {
                comend.Connection = conect;
                conect.Open();
                comend.CommandText = "EXEC PromoteStudents @Studies, @Semester";
                comend.Parameters.AddWithValue("Studies", request.Studies);
                comend.Parameters.AddWithValue("Semester", request.Semester);
                var dr = comend.ExecuteReader();

                if (dr.Read())
                {
                    return new PromoteStudentsResponse
                    {
                        IdEnrollment = (int)dr["IdEnrollment"],
                        Semester = (int)dr["Semester"],
                        IdStudy = (int)dr["IdStudy"],
                        StartDate = (DateTime)dr["StartDate"]
                    };
                }
                else
                {
                    return null;
                }
            }
        }
        public Student GetStudent(String id)
        {

            using (var conect = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var comend = new SqlCommand())
            {
                var st = new Student();
                comend.Connection = conect;
                conect.Open();
                comend.CommandText = "SELECT * FROM StudentAPBD s LEFT JOIN ENROLLMENT e ON s.IdEnrollment = e.IdEnrollment LEFT JOIN STUDIES st on e.IdStudy = st.IdStudy WHERE IndexNumber LIKE @id";
                comend.Parameters.AddWithValue("id", id);
                var dr = comend.ExecuteReader();
                if (dr.Read())
                {
                    return new Student
                    {
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        IndexNumber = dr["IndexNumber"].ToString(),
                        BirthDate = Convert.ToDateTime(dr["BirthDate"].ToString()),
                        Studies = dr["Name"].ToString(),
                        Semester = Convert.ToInt32(dr["Semester"].ToString())
                    };
                }
                else
                {
                    return null;
                }
            }
        }
        public IEnumerable<Student> GetStudents()
        {

            using (var conect = new SqlConnection("Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s17057;Integrated Security=True"))
            using (var comend = new SqlCommand())
            {
                var list = new List<Student>();
                comend.Connection = conect;
                conect.Open();
                comend.CommandText = "SELECT * FROM StudentAPBD s LEFT JOIN ENROLLMENT e ON s.IdEnrollment = e.IdEnrollment LEFT JOIN STUDIES st on e.IdStudy = st.IdStudy";
                var dr = comend.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        list.Add(new Student
                        {
                            FirstName = dr["FirstName"].ToString(),
                            LastName = dr["LastName"].ToString(),
                            IndexNumber = dr["IndexNumber"].ToString(),
                            BirthDate = Convert.ToDateTime(dr["BirthDate"].ToString()),
                            Studies = dr["Name"].ToString(),
                            Semester = Convert.ToInt32(dr["Semester"].ToString())
                        });
                    }
                    return list;
                }
                else
                {
                    return null;
                }
                
            }
        }
    }
}
