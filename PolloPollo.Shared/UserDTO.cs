﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PolloPollo.Shared
{
    public class UserDTO
    {
        public int UserId { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string FirstName { get; set; }

        public string Surname { get; set; }

        public string Country { get; set; }

        public string Role { get; set; }

        public string Description { get; set; }

        public string City { get; set; }

        public string Thumbnail { get; set; }
    }
}
