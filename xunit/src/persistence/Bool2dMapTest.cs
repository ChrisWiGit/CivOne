using System;
using CivOne.Persistence.Yaml;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
    public class Bool2DMapTest
    {
        private Bool2dMap _testee;

        public Bool2DMapTest()
        {
        }

        [Fact]
        public void TestConstructorWidthHeight()
		{
			_testee = new Bool2dMap(5, 5);
            var actual = (bool[,])_testee;
            bool[,] expected = new bool[5, 5];
            Assert.Equal(expected, actual);
		}

        [Fact]
        public void TestConstructorBool2dArray()
        {
            bool[,] data = new bool[5, 5];
            data[2, 3] = true;
            _testee = new Bool2dMap(data);
            var actual = (bool[,])_testee;
            Assert.Equal(data, actual);
        }

        [Fact]
        public void TestConstructorBool2dJaggedArray()
        {
            bool[][] data = new bool[5][];
            for (int i = 0; i < 5; i++)
                data[i] = new bool[5];
            data[2][3] = true;
            _testee = new Bool2dMap(data);
            var actual = (bool[,])_testee;
            bool[,] expected = new bool[5, 5];
            expected[2, 3] = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstructorCopy()
        {            
            bool[,] data = new bool[5, 5];
            data[2, 3] = true;
            _testee = new Bool2dMap(data);
            var copy = new Bool2dMap(_testee);
            var actual = (bool[,])copy;
            bool[,] expected = new bool[5, 5];
            expected[2, 3] = true;
            Assert.Equal(expected, actual);

            // modify original
            data[2, 3] = false;
            // copy should not change
            actual = (bool[,])copy;
            Assert.Equal(expected, actual);
        }

        //rows
        [Fact]
        public void TestConstructorRows()
		{
			string[] rows = { "00000", "01000" };
            _testee = new Bool2dMap(rows);
            var actual = (bool[,])_testee;
            bool[,] expected = new bool[5, 2];
            expected[1, 1] = true;
            Assert.Equal(expected, actual);
		}

        [Fact]
        public void TestIndexer()
        {
            _testee = new Bool2dMap(5, 5);
            _testee[2, 3] = true;
            var actual = (bool[,])_testee;
            bool[,] expected = new bool[5, 5];
            expected[2, 3] = true;
            Assert.Equal(expected, actual);
        }

        // row indexer single value
        [Fact]
        public void TestRowIndexer()
        {
            _testee = new Bool2dMap(5, 2);
            _testee[1] = [true, false, false, true, false];

            Assert.Equal(new bool[] { false, false, false, false, false }, _testee[0]);
            Assert.Equal(new bool[] { true, false, false, true, false }, _testee[1]);
        }

        [Fact]
        public void TestYamlSerialization()
        {
            _testee = new Bool2dMap(5, 2);
            _testee[1] = [true, false, false, true, false];
            // var serializer = new Serializer();
            var serializer = new SerializerBuilder()
                .WithTypeConverter(new Bool2dMapYamlTypeConverter()).Build();
            var yaml = serializer.Serialize(_testee);
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new Bool2dMapYamlTypeConverter()).Build();
            var deserialized = deserializer.Deserialize<Bool2dMap>(yaml);
            Assert.Equal(_testee.Rows, deserialized.Rows);
        }

        [Fact]
        public void TestYamlWriteNullThrowsArgumentNullException()
        {
            var converter = new Bool2dMapYamlTypeConverter();

            Assert.Throws<ArgumentNullException>(() => converter.WriteYaml(null!, null, typeof(Bool2dMap), null!));
        }

        [Fact]
        public void TestYamlWriteWrongTypeThrowsArgumentException()
        {
            var converter = new Bool2dMapYamlTypeConverter();

            Assert.Throws<ArgumentException>(() => converter.WriteYaml(null!, new object(), typeof(object), null!));
        }

        [Fact]
        public void TestYamlReadNullReturnsEmptyMap()
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new Bool2dMapYamlTypeConverter()).Build();

            var deserialized = deserializer.Deserialize<Bool2dMap>("null");

            Assert.Equal(0, deserialized.Width());
            Assert.Equal(0, deserialized.Height());
            Assert.Empty(deserialized.Rows);
        }

        [Fact]
        public void TestYamlReadEmptyArrayReturnsEmptyMap()
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new Bool2dMapYamlTypeConverter()).Build();

            var deserialized = deserializer.Deserialize<Bool2dMap>("[]");

            Assert.Equal(0, deserialized.Width());
            Assert.Equal(0, deserialized.Height());
            Assert.Empty(deserialized.Rows);
        }
    }
}