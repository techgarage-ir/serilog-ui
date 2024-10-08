﻿using System;
using Serilog.Ui.Core.Attributes;
using Serilog.Ui.Core.Models;

namespace Serilog.Ui.MySqlProvider;

/// <summary>
/// MySql/MariaDb Log Model. <br />
/// <see cref="RowNo"/>, <see cref="Level"/>, <see cref="Message"/>, <see cref="Timestamp"/>
/// columns can't be overridden and removed from the model, due to query requirements. <br />
/// To remove a field, apply <see cref="RemovedColumnAttribute"/> on it.
/// To add a field, register the property with the correct datatype on the child class and the sink.
/// </summary>
public class MySqlLogModel : LogModel
{
    public override sealed int RowNo => base.RowNo;

    public override sealed string? Level { get; set; }

    public string LogLevel { get; set; } = string.Empty;

    public override sealed string? Message { get; set; } = string.Empty;

    public override sealed DateTime Timestamp { get; set; }

    public override string PropertyType => "json";
}