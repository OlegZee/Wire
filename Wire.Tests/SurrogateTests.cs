using System.IO;
using System.Runtime.Serialization;
using Xunit;

namespace Wire.Tests
{
    public interface IOriginal
    {
        ISurrogate ToSurrogate();
    }

    public interface ISurrogate
    {
        IOriginal FromSurrogate();
    }

    public class Foo : IOriginal
    {
        public string Bar { get; set; }

        public ISurrogate ToSurrogate()
        {
            return new FooSurrogate
            {
                Bar = Bar
            };
        }
    }

    public class FooSurrogate : ISurrogate
    {
        public string Bar { get; set; }

        public IOriginal FromSurrogate()
        {
            return Restore();
        }

        public static FooSurrogate FromFoo(Foo foo)
        {
            return new FooSurrogate
            {
                Bar = foo.Bar
            };
        }

        public Foo Restore()
        {
            return new Foo
            {
                Bar = Bar
            };
        }
    }

    public class SurrogateTests
    {
        [Fact]
        public void CanSerializeWithSurrogate()
        {
            var surrogateHasBeenInvoked = false;
            var surrogates = new[]
            {
                Surrogate.Create<Foo, FooSurrogate>(FooSurrogate.FromFoo, surrogate =>
                {
                    surrogateHasBeenInvoked = true;
                    return surrogate.Restore();
                })
            };
            var stream = new MemoryStream();
            var serializer = new Serializer(new SerializerOptions(surrogates: surrogates));
            var foo = new Foo
            {
                Bar = "I will be replaced!"
            };

            serializer.Serialize(foo, stream);
            stream.Position = 0;
            var actual = serializer.Deserialize<Foo>(stream);
            Assert.Equal(foo.Bar, actual.Bar);
            Assert.True(surrogateHasBeenInvoked);
        }

        [Fact]
        public void CanSerializeWithInterfaceSurrogate()
        {
            var surrogateHasBeenInvoked = false;
            var surrogates = new[]
            {
                Surrogate.Create<IOriginal, ISurrogate>(from => from.ToSurrogate(), surrogate =>
                {
                    surrogateHasBeenInvoked = true;
                    return surrogate.FromSurrogate();
                })
            };
            var stream = new MemoryStream();
            var serializer = new Serializer(new SerializerOptions(surrogates: surrogates));
            var foo = new Foo
            {
                Bar = "I will be replaced!"
            };

            serializer.Serialize(foo, stream);
            stream.Position = 0;
            var actual = serializer.Deserialize<Foo>(stream);
            Assert.Equal(foo.Bar, actual.Bar);
            Assert.True(surrogateHasBeenInvoked);
        }

		public class SameInstance
		{
			[Fact]
			public void Can_deserialize_surrogates_which_refer_the_same_instance()
			{
				var surrogateHasBeenInvoked = false;
				var surrogates = new[]
				{
					Surrogate.Create<Foo, FooSurrogate>(FooSurrogate.FromFoo, surrogate =>
					{
						surrogateHasBeenInvoked = true;
						return surrogate.Restore();
					})
				};
				var stream = new MemoryStream();
				var serializer = new Serializer(new SerializerOptions(surrogates: surrogates, preserveObjectReferences: true));

				var foo = new Foo { Bar = "I will be replaced!" };
				var fooArray = new Foo[] { foo, foo };

				serializer.Serialize(fooArray, stream);

				var bytes = stream.ToArray();
				Foo[] actualArray;
				using (var ms = new MemoryStream(bytes))
				{
					var ret = serializer.Deserialize<object>(ms);
					actualArray = (Foo[])ret;
				}

				Assert.Equal(actualArray[0], actualArray[1]);
				Assert.True(surrogateHasBeenInvoked);
			}

			public class Foo : IOriginal
			{
				public string Bar { get; set; }

				public ISurrogate ToSurrogate()
				{
					return new FooSurrogate
					{
						Bar = Bar
					};
				}

				public override bool Equals(object obj)
				{
					var other = obj as Foo;
					if (other == null) return false;
					return Bar == other.Bar;
				}
			}

			public class FooSurrogate : ISurrogate
			{
				public string Bar { get; set; }

				public IOriginal FromSurrogate()
				{
					return Restore();
				}

				public static FooSurrogate FromFoo(Foo foo)
				{
					return new FooSurrogate
					{
						Bar = foo.Bar
					};
				}

				public Foo Restore()
				{
					return new Foo
					{
						Bar = Bar
					};
				}

				public override bool Equals(object obj)
				{
					var other = obj as FooSurrogate;
					if (other == null) return false;
					return Bar == other.Bar;
				}
			}
		}

	}
}
