﻿
[AttributeUsage(AttributeTargets.Property)]
public class SnowflakeAttribute : Attribute
{
	public bool Enable { get; set; } = true;
}