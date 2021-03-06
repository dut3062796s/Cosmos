﻿using System;
using Cosmos.Logging.Events;

namespace Cosmos.Logging.ExtraSupports {
    public class ExtraMessageProperty : IMessageProperty {
        public ExtraMessageProperty(string name, MessagePropertyValue value) {
            CheckParams(name, value);
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public MessagePropertyValue Value { get; }

        private static void CheckParams(string name, MessagePropertyValue value) {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));
        }

        public override string ToString() {
            return $"{Name}: {Value.ToString()}";
        }
    }
}