using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VaporObservables.Tests
{
    public class ObservableTests
    {
        [Serializable]
        internal struct ObservableTestStruct
        {
            public int Value1;
            public float Value2;

            public ObservableTestStruct(int v1, float v2) : this()
            {
                Value1 = v1;
                Value2 = v2;
            }
        }

        internal class ObservableClassTest : ObservableClass
        {
            public ObservableClassTest(string className) : base(className)
            {

            }

            protected override void SetupFields()
            {
                AddField<float>("Hp", true, 100);
                AddField<float>("Mp", true, 50);
            }
        }

        internal class ObservedClassTest : IObservedClass
        {
            public void SetupFields(ObservableClass @class)
            {
                @class.AddField<float>("Hp", true, 100);
                @class.AddField<float>("Mp", true, 50);
            }
        }

        [Test]
        public void ObservableValueChangedTests()
        {
            int callbackCount = 0;
            var observables = new List<Observable>()
            {
                new Observable<bool>("Bool", true, true).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<char>("Char", true, '+').WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<byte>("Byte", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<sbyte>("SByte", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<short>("Short", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<ushort>("UShort", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<int>("Int", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<uint>("UInt", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<float>("Float", true, 1.0f).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<long>("Long", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<ulong>("ULong", true, 1).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<double>("Double", true, 1.0).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<decimal>("Decimal", true, 1.0m).WithChanged((obs, old) =>
                {
                    _Callback();
                }),

                new Observable<Vector2>("Vector2", true, Vector2.one).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<Vector2Int>("Vector2Int", true, Vector2Int.one).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<Vector3>("Vector3", true, Vector3.one).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<Vector3Int>("Vector3Int", true, Vector3Int.one).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<Color>("Color", true, Color.red).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<Quaternion>("Quaternion", true, Quaternion.Euler(0,90,0)).WithChanged((obs, old) =>
                {
                    _Callback();
                }),

                // Unhandled Attempt
                new Observable<(int, float)>("Unhandled Attempt 1", true, new (1, 1.0f)).WithChanged((obs, old) =>
                {
                    _Callback();
                }),
                new Observable<ObservableTestStruct>("Unhandled Attempt 2", true, new (1, 1.0f)).WithChanged((obs, old) =>
                {
                    //Debug.Log($"New {obs.Value} Old {old}");
                    _Callback();
                }),
            };

            foreach (var obs in observables)
            {

                switch (obs)
                {
                    case Observable<bool> obsB:
                        obsB.Value = !obsB.Value;
                        break;
                    case Observable<char> obsB:
                        obsB.Value = '-';
                        break;
                    case Observable<byte> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<sbyte> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<short> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<ushort> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<int> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<uint> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<float> obsB:
                        obsB.Value = 2.0f;
                        break;
                    case Observable<long> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<ulong> obsB:
                        obsB.Value = 2;
                        break;
                    case Observable<double> obsB:
                        obsB.Value = 2.0;
                        break;
                    case Observable<decimal> obsB:
                        obsB.Value = 2.0m;
                        break;
                    case Observable<Vector2> obsB:
                        obsB.Value = 2 * Vector2.one;
                        break;
                    case Observable<Vector2Int> obsB:
                        obsB.Value = 2 * Vector2Int.one;
                        break;
                    case Observable<Vector3> obsB:
                        obsB.Value = 2 * Vector3.one;
                        break;
                    case Observable<Vector3Int> obsB:
                        obsB.Value = 2 * Vector3Int.one;
                        break;
                    case Observable<Color> obsB:
                        obsB.Value = Color.blue;
                        break;
                    case Observable<Quaternion> obsB:
                        obsB.Value = Quaternion.Euler(-90, 0, 0);
                        break;
                    case Observable<(int, float)> obsB:
                        obsB.Value = new(2, 2.0f);
                        break;
                    case Observable<ObservableTestStruct> obsB:
                        obsB.Value = new(2, 2.0f);
                        break;
                    default:
                        break;
                }
            }

            Assert.That(callbackCount, Is.EqualTo(observables.Count));

            void _Callback()
            {
                callbackCount++;
            }
        }

        [Test]
        public void ObservableSerializationTests()
        {
            var observables = new List<Observable>()
            {
                new Observable<bool>("Bool", true, true),
                new Observable<char>("Char", true, '+'),
                new Observable<byte>("Byte", true, 1),
                new Observable<sbyte>("SByte", true, 1),
                new Observable<short>("Short", true, 1),
                new Observable<ushort>("UShort", true, 1),
                new Observable<int>("Int", true, 1),
                new Observable<uint>("UInt", true, 1),
                new Observable<float>("Float", true, 1.0f),
                new Observable<long>("Long", true, 1),
                new Observable<ulong>("ULong", true, 1),
                new Observable<double>("Double", true, 1.0),
                new Observable<decimal>("Decimal", true, 1.0m),

                new Observable<Vector2>("Vector2", true, Vector2.one),
                new Observable<Vector2Int>("Vector2Int", true, Vector2Int.one),
                new Observable<Vector3>("Vector3", true, Vector3.one),
                new Observable<Vector3Int>("Vector3Int", true, Vector3Int.one),
                new Observable<Color>("Color", true, Color.red),
                new Observable<Quaternion>("Quaternion", true, Quaternion.Euler(0,90,0)),

                // Unhandled Attempt
                new Observable<(int, float)>("Unhandled Attempt 1", true, new (1, 1.0f)),
                new Observable<ObservableTestStruct>("Unhandled Attempt 2", true, new (1, 1.0f)),
            };

            foreach (var obs in observables)
            {
                var json = obs.SaveAsJson();
                Assert.That(json, Is.Not.Null.Or.Empty);

                var load = Observable.Load(json);
                Assert.That(load, Is.TypeOf(obs.GetType()));
                Assert.That(load.Name, Does.Match(obs.Name));
                Assert.That(load.GetValueBoxed(), Is.EqualTo(obs.GetValueBoxed()));
            }            
        }

        [Test]
        public void ObservableClassSerializationTests()
        {
            var @class = new ObservableClassTest("Status");
            @class.SetFieldValue<float>("Hp", 200);
            var json = @class.SaveAsJson();
            Assert.That(json, Is.Not.Null.Or.Empty);

            var data = ObservableClass.Load(json);
            var load = new ObservableClassTest("Status");
            load.Load(data);
            Assert.That(load, Is.TypeOf(@class.GetType()));
            Assert.That(load.Name, Does.Match(@class.Name));
            Assert.That(load.GetField<Observable<float>>("Hp").Value, Is.EqualTo(@class.GetField<Observable<float>>("Hp").Value));
            Assert.That(load.GetField<Observable<float>>("Mp").Value, Is.EqualTo(@class.GetField<Observable<float>>("Mp").Value));

            var @class2 = new ObservableClass<ObservedClassTest>("Status2", new ObservedClassTest());
            @class2.SetFieldValue<float>("Hp", 200);
            var json2 = @class2.SaveAsJson();
            Assert.That(json2, Is.Not.Null.Or.Empty);

            var data2 = ObservableClass.Load(json2);
            var load2 = new ObservableClass<ObservedClassTest>("Status2", new ObservedClassTest());
            load2.Load(data2);
            Assert.That(load2, Is.TypeOf(@class2.GetType()));
            Assert.That(load2.Name, Does.Match(@class2.Name));
            Assert.That(load2.GetField<Observable<float>>("Hp").Value, Is.EqualTo(@class2.GetField<Observable<float>>("Hp").Value));
            Assert.That(load2.GetField<Observable<float>>("Mp").Value, Is.EqualTo(@class2.GetField<Observable<float>>("Mp").Value));
        }
    }
}
