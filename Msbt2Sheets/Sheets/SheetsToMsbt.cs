﻿using Google.Apis.Sheets.v4.Data;
using Msbt2Sheets.Lib.Formats;
using Msbt2Sheets.Lib.Formats.FileComponents;
using Color = System.Drawing.Color;

namespace Msbt2Sheets.Sheets;

public class SheetsToMsbt
{
    public static void Create(GoogleSheetsManager sheetsManager)
    {
        Console.Clear();
        ConsoleUtils.WriteLineColored("Enter your spreadsheet's ID.\n(It's in the link: https://docs.google.com/spreadsheets/d/|1pRFVKt4fNnWHKf8kIpSk0qmu7u-EdHEUGwkTP9Kzq3A|/edit)", ConsoleColor.Cyan);
        string spreadsheetId = Console.ReadLine();
        
        Console.WriteLine("Loading metadata from the spreadsheet...");
        Spreadsheet spreadsheet = sheetsManager.GetSpreadSheet(spreadsheetId);
        
        List<string> requestRanges = new List<string>();
        foreach (Sheet sheet in spreadsheet.Sheets)
        {
            requestRanges.Add($"{sheet.Properties.Title}!A:ZZZ");
        }
        
        Console.WriteLine("Loading cell data from the spreadsheet...");
        BatchGetValuesResponse valueRanges = sheetsManager.GetMultipleValues(spreadsheetId, requestRanges.ToArray());
        var spreadsheetValues = valueRanges.ValueRanges.ToList();
        var sheets = ValueRangesToStringLists(spreadsheetValues);

        ParsingOptions options = ObtainOptions(spreadsheet, sheets);

        MSBP msbp = ObtainMsbp(spreadsheet, sheets);
        
        ConsoleUtils.Exit();
    }

    static List<List<List<string>>> ValueRangesToStringLists(List<ValueRange> valueRanges)
    {
        List<List<List<string>>> sheets = new();
        foreach (var valueRange in valueRanges)
        {
            var rows = valueRange.Values;
            var sheet = new List<List<string>>();
            foreach (var row in rows)
            {
                List<string> stringRow = new();
                foreach (var cell in row)
                {
                    stringRow.Add((string)cell);
                }
                sheet.Add(stringRow);
            }
            sheets.Add(sheet);
        }

        return sheets;
    }

    static ParsingOptions ObtainOptions(Spreadsheet spreadsheet, List<List<List<string>>> sheets)
    {
        int sheetId = IndexOfSheetByName(spreadsheet, "#Settings");
        ParsingOptions options = new();
        
        foreach (var row in sheets[sheetId])
        {
            if (row[0] == "Add linebreaks after pagebreaks")
            {
                options.AddLinebreaksAfterPagebreaks = row[1] == "TRUE";
            }
            if (row[0] == "Color identification")
            {
                options.ColorIdentification = row[1];
            }
        }

        return options;
    }
    
    static int IndexOfSheetByName(Spreadsheet spreadsheet, string sheetName)
    {
        int sheetId = -1;
        for (int i = 0; i < spreadsheet.Sheets.Count; i++)
        {
            if (spreadsheet.Sheets[i].Properties.Title == sheetName)
            {
                sheetId = i;
                break;
            }
        }

        return sheetId;
    }

    static MSBP ObtainMsbp(Spreadsheet spreadsheet, List<List<List<string>>> sheets)
    {
        MSBP msbp = new();

        ObtainMsbpColors(spreadsheet, sheets, msbp);
        ObtainMsbpStyles(spreadsheet, sheets, msbp);

        return msbp;
    }

    static void ObtainMsbpColors(Spreadsheet spreadsheet, List<List<List<string>>> sheets, MSBP msbp)
    {
        int colorSheetId = IndexOfSheetByName(spreadsheet, "#BaseColors");
        if (colorSheetId != -1)
        {
            msbp.HasCLR1 = true;
            Sheet sheet = spreadsheet.Sheets[colorSheetId];
            if (sheet.Properties.GridProperties.ColumnCount == 2)
            {
                msbp.HasCLB1 = true;

                foreach (var row in sheets[colorSheetId])
                {
                    msbp.Colors.Add(row[0], ColorStringToColor(row[1]));
                }
            }
            else
            {
                int counter = 0;
                foreach (var row in sheets[colorSheetId])
                {
                    msbp.Colors.Add(counter.ToString(), ColorStringToColor(row[0]));
                    counter++;
                }
            }
        }
    }

    static Color ColorStringToColor(string colorStr)
    {
        byte[] colorBytes = Convert.FromHexString(colorStr[1..colorStr.Length]);
        return Color.FromArgb(colorBytes[3], colorBytes[0], colorBytes[1], colorBytes[2]);
    }

    static void ObtainMsbpStyles(Spreadsheet spreadsheet, List<List<List<string>>> sheets, MSBP msbp)
    {
        int styleSheetId = IndexOfSheetByName(spreadsheet, "#Styles");
        if (styleSheetId != -1)
        {
            msbp.HasSYL3 = true;
            Sheet sheet = spreadsheet.Sheets[styleSheetId];
            var rows = sheets[styleSheetId];
            var headerRow = rows[0];
            if (headerRow[0] == "Name")
            {
                msbp.HasSLB1 = true;
            }

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];

                int colorId = msbp.HasCLR1 ? GetColorIdFromName(row[4], msbp) : Convert.ToInt32(row[4]);

                msbp.Styles.Add(new Style(row[0], Convert.ToInt32(row[1]), Convert.ToInt32(row[2]), Convert.ToInt32(row[3]), colorId));
            }
        }
    }

    static int GetColorIdFromName(string name, MSBP msbp)
    {
        int id = 0;
        foreach (var color in msbp.Colors)
        {
            if (color.Key == name)
            {
                return id;
            }

            id++;
        }

        return Convert.ToInt32(name);
    }
}