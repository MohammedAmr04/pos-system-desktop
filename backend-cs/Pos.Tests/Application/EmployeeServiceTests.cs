using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakeEmployeeRepository : IEmployeeRepository
    {
        public readonly List<Employee> Stored = new List<Employee>();

        public List<Employee> GetAll() { return Stored.ToList(); }
        public PagedResult<Employee> GetPaged(int page, int pageSize, string query) { return new PagedResult<Employee>(); }
        public Employee GetById(string id) { return Stored.FirstOrDefault(e => e.Id == id); }
        public Employee Create(Employee employee)
        {
            employee.Id = "employee-" + (Stored.Count + 1);
            Stored.Add(employee);
            return employee;
        }
        public Employee Update(Employee employee) { return employee; }
    }

    public class EmployeeServiceTests
    {
        [Fact]
        public void Create_Rejects_Missing_Name()
        {
            var service = new EmployeeService(new FakeEmployeeRepository());

            Assert.Throws<DomainValidationException>(() =>
                service.Create(new CreateEmployeeRequest { Name = "  " }));
        }

        [Fact]
        public void Create_Trims_Name_And_Activates()
        {
            var service = new EmployeeService(new FakeEmployeeRepository());

            var employee = service.Create(new CreateEmployeeRequest { Name = "  Ahmed  ", Phone = " 010 " });

            Assert.Equal("Ahmed", employee.Name);
            Assert.Equal("010", employee.Phone);
            Assert.True(employee.IsActive);
        }

        [Fact]
        public void GetActive_Excludes_Inactive()
        {
            var repo = new FakeEmployeeRepository();
            repo.Stored.Add(new Employee { Id = "e1", Name = "Active", IsActive = true });
            repo.Stored.Add(new Employee { Id = "e2", Name = "Inactive", IsActive = false });
            var service = new EmployeeService(repo);

            var active = service.GetActive();

            Assert.Single(active);
            Assert.Equal("e1", active[0].Id);
        }

        [Fact]
        public void Update_Unknown_Id_Throws_NotFound()
        {
            var service = new EmployeeService(new FakeEmployeeRepository());

            Assert.Throws<NotFoundException>(() =>
                service.Update("missing", new UpdateEmployeeRequest { Name = "Nope" }));
        }
    }
}
