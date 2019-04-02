﻿using System.ComponentModel.DataAnnotations;

namespace PolloPollo.Shared
{
    public class ProductUpdateDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public bool Available { get; set; }
    }
}
