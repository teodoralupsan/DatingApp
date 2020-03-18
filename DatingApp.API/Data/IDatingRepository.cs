using DatingApp.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.API.Data
{
    public interface IDatingRepository
    {
        void Add<T>(T entity) where T : class;

        void Delete<T>(T entity) where T : class;
        
        Task<User> GetUser(int id);

        Task<IEnumerable<User>> GetUsers();

        Task<bool> SaveAll();

        Task<Photo> GetPhoto(int id);
    }
}
