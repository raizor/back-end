﻿using System.Collections.Generic;
using PolloPollo.Entities;

namespace PolloPollo.Repository
{
    public interface IUserRepository
    {
        User Authenticate(string firstname, string surname, string password);
        IEnumerable<User> GetAll();
    }
}
