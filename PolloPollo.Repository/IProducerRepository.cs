﻿using PolloPollo.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolloPollo.Repository
{
    public interface IProducerRepository
    {
        Task<ProducerDTO> CreateAsync(int userId);

        Task<ProducerDTO> FindAsync(int userId);

        Task<bool> DeleteAsync(int userId);

        Task<bool> UpdateAsync(UserUpdateDTO dto);

        IQueryable<ProducerDTO> Read();
    }
}