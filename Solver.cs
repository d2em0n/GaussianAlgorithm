using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;

namespace GaussAlgorithm;

public class Solver
{
    static double[] MarkRow(double[][] matrix, double[] freeMembers, int cols, int row, int col)
    {
        var unitedRow = new double[cols + 1];
        for (var k = 0; k < cols; k++)
            unitedRow[k] = matrix[row][k] / matrix[row][col];
        unitedRow[cols] = freeMembers[row] / matrix[row][col];
        return unitedRow;
    }

    static void ZeroColumnBelow(double[][] matrix, double[] freeMembers, int rows, int cols, int row, int col)
    {
        for (var k = row + 1; k < rows; k++)
        {
            if (Math.Abs(matrix[k][col]) < 1e-6) continue;
            var factor = matrix[k][col] / matrix[row][col] * (-1);
            for (var l = 0; l < cols; l++)
                matrix[k][l] += factor * matrix[row][l];
            freeMembers[k] += factor * freeMembers[row];
        }
    }

    static void CheckHasNoSolutions(double[][] initialMatrix, double[][] matrix, double[] freeMembers, int rows)
    {
        for (var i = 0; i < rows; i++)
        {
            if (matrix[i].All(x => Math.Abs(x) < 1e-6) && Math.Abs(freeMembers[i]) > 1e-6)
                throw new NoSolutionException(initialMatrix, freeMembers, matrix);
        }
    }

    static Dictionary<int, double[]> NumberTheEquations(double[][] matrix, double[] freeMembers, int rows, int cols)
    {
        var marked = new List<int>();
        var d = new Dictionary<int, double[]>();
        for (var j = 0; j < cols; j++)
        {
            for (var i = 0; i < rows; i++)
            {
                if (matrix[i][j] != 0 && !marked.Contains(i))
                {
                    d[j] = MarkRow(matrix, freeMembers, cols, i, j);
                    marked.Add(i);
                    ZeroColumnBelow(matrix, freeMembers, rows, cols, i, j);
                    break;
                }
            }
        }
        return d;
    }

    static void ZeroUpperTriangle(Dictionary<int, double[]> significantRows, int cols)
    {
        foreach (var col in significantRows.Keys)
        {
            ZeroToTheRight(significantRows, cols, col);
        }
    }

    static void ZeroToTheRight(Dictionary<int, double[]> significantRows, int cols, int key)
    {
        var row = significantRows[key];
        for (var i = key + 1; i < cols; i++)
        {
            if (row[i] == 0) continue;
            if (!significantRows.ContainsKey(i))
            {
                row[i] = 0;
                continue;
            }
            row = row
                .Zip(significantRows[i]
                        .Select(x => x * row[i] * (-1)))
                    .Select(x => x.Second + x.First)
                    .ToArray();
        }
        significantRows[key] = row;
    }

    public double[] Solve(double[][] matrix, double[] freeMembers)
    {
        var initial = (double[][])matrix.Clone();
        var rows = matrix.Length;
        var cols = matrix[0].Length;
        var solution = new double[cols];
        var numberedEquations = NumberTheEquations(matrix, freeMembers, rows, cols);
        CheckHasNoSolutions(initial, matrix, freeMembers, rows);
        ZeroUpperTriangle(numberedEquations, cols);

        foreach (var i in numberedEquations.Keys)
            solution[i] = numberedEquations[i][cols];
        return solution;
    }
}
[TestFixture]
public class MyTests
{
    [Test]
    public void FromTaskTest()
    {
        var matrix = new double[][]
        {
            new double[] {1, 2, 3 },
            new double[] { 1, 1, 5},
            new double[] { 2, -1, 2}
        };
        var freeMembers = new double[] { 1, -1, 6 };
        var solver = new Solver();
        var result = solver.Solve(matrix, freeMembers);
        Assert.AreEqual(result, new double[] { 4, 0, -1 });
    }
}