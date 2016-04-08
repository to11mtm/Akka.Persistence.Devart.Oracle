using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devart.Data.Oracle;
using FluentAssertions;
using TestStack.BDDfy;
using Xunit;

namespace Akka.Persistence.Devart.Oracle
{
    public class OracleConnectionTest
    {
        private string _connectionString;
        private OracleConnection _connection;

        public void GivenTestDbConnectionDetails()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
        }

        public void WhenOpeningAConnection()
        {
            _connection = new OracleConnection(_connectionString);
            _connection.Open();
        }

        public void ThenItShouldOpenSuccessfully()
        {
            _connection.State.Should().Be(ConnectionState.Open);
        }

        public void TearDown()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }


        [Fact]
        public void Execute()
        {
            this.BDDfy();
        }
        
    }
}
