﻿using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Xamarin.Forms;
using Xamarin.Platform.Handlers.Tests;
using Xamarin.Platform.Layouts;
using static Xamarin.Platform.Handlers.UnitTests.Layouts.LayoutTestHelpers;

namespace Xamarin.Platform.Handlers.UnitTests.Layouts
{
	[TestFixture(Category = TestCategory.Layout)]
	public class GridLayoutManagerTests 
	{
		// TODO ezhart Include row/col spacing tests

		// TODO ezhart Need a test to assert that auto rows/cols with no controls in them get 0
		// TOOD ezhart What happens if There is row/col spacing and an auto with no controls is in the middle? is the spacing around it included (2 spacings), or just one spacing? (I think just one is least astonishing)

		IGridLayout CreateGridLayout(int rowSpacing = 0, int colSpacing = 0,
			string rows = null, string columns = null)
		{
			IEnumerable<IGridRowDefinition> rowDefs = null;
			IEnumerable<IGridColumnDefinition> colDefs = null;

			if (rows != null)
			{
				rowDefs = CreateTestRows(rows.Split(","));
			}

			if (columns != null)
			{
				colDefs = CreateTestColumns(columns.Split(","));
			}

			var grid = Substitute.For<IGridLayout>();

			grid.RowSpacing.Returns(rowSpacing);
			grid.ColumnSpacing.Returns(colSpacing);

			SubRowDefs(grid, rowDefs);
			SubColDefs(grid, colDefs);

			return grid;
		}

		void SubRowDefs(IGridLayout grid, IEnumerable<IGridRowDefinition> rows = null) 
		{
			if (rows == null)
			{
				var rowDef = Substitute.For<IGridRowDefinition>();
				rowDef.Height.Returns(GridLength.Auto);
				var rowDefs = new List<IGridRowDefinition>
				{
					rowDef
				};
				grid.RowDefinitions.Returns(rowDefs);
			}
			else
			{
				grid.RowDefinitions.Returns(rows);
			}
		}

		void SubColDefs(IGridLayout grid, IEnumerable<IGridColumnDefinition> cols = null)
		{
			if (cols == null)
			{
				var colDefs = CreateTestColumns("auto");
				grid.ColumnDefinitions.Returns(colDefs);
			}
			else
			{
				grid.ColumnDefinitions.Returns(cols);
			}
		}

		List<IGridColumnDefinition> CreateTestColumns(params string[] columnWidths) 
		{
			var converter = new GridLengthTypeConverter();

			var colDefs = new List<IGridColumnDefinition>();

			foreach (var width in columnWidths)
			{
				var gridLength = converter.ConvertFromInvariantString(width);
				var colDef = Substitute.For<IGridColumnDefinition>();
				colDef.Width.Returns(gridLength);
				colDefs.Add(colDef);
			}

			return colDefs;
		}

		List<IGridRowDefinition> CreateTestRows(params string[] rowHeights)
		{
			var converter = new GridLengthTypeConverter();

			var rowDefs = new List<IGridRowDefinition>();

			foreach (var height in rowHeights)
			{
				var gridLength = converter.ConvertFromInvariantString(height);
				var rowDef = Substitute.For<IGridRowDefinition>();
				rowDef.Height.Returns(gridLength);
				rowDefs.Add(rowDef);
			}

			return rowDefs;
		}

		void SetLocation(IGridLayout grid, IView view, int row = 0, int col = 0, int rowSpan = 1, int colSpan = 1) 
		{
			grid.GetRow(view).Returns(row);
			grid.GetRowSpan(view).Returns(rowSpan);
			grid.GetColumn(view).Returns(col);
			grid.GetColumnSpan(view).Returns(colSpan);
		}

		void MeasureAndArrange(IGridLayout grid, double widthConstraint, double heightConstraint) 
		{
			var manager = new GridLayoutManager(grid);
			var measuredSize = manager.Measure(widthConstraint, heightConstraint);
			manager.ArrangeChildren(new Rectangle(Point.Zero, measuredSize));
		}

		void AssertArranged(IView view, double x, double y, double width, double height)
		{
			var expected = new Rectangle(x, y, width, height);
			view.Received().Arrange(Arg.Is(expected));
		}

		[Test]
		public void OneAutoRowOneAutoColumn() 
		{
			// A one-row, one-column grid
			var grid = CreateGridLayout();

			// A 100x100 IView
			var view = CreateTestView(new Size(100, 100));

			// Set up the grid to have a single child
			AddChildren(grid, view);

			// Set up the row/column values and spans
			SetLocation(grid, view);

			MeasureAndArrange(grid, double.PositiveInfinity, double.PositiveInfinity);

			// We expect that the only child of the grid will be given its full size
			AssertArranged(view, 0, 0, 100, 100);
		}

		[Test]
		public void TwoAbsoluteColumnsOneAbsoluteRow()
		{
			var grid = CreateGridLayout(columns: "100, 100", rows: "10");

			var viewSize = new Size(10, 10);

			var view0 = CreateTestView(viewSize);
			var view1 = CreateTestView(viewSize);

			AddChildren(grid, view0, view1);

			SetLocation(grid, view0);
			SetLocation(grid, view1, col: 1);

			// Assuming no constraints on space
			MeasureAndArrange(grid, double.PositiveInfinity, double.NegativeInfinity);

			// Column width is 100, so despite the view being 10 wide, we expect it to be arranged with width 100
			AssertArranged(view0, 0, 0, 100, viewSize.Height);

			// Since the first column is 100 wide, we expect the view in the second column to start at x = 100
			AssertArranged(view1, 100, 0, 100, viewSize.Height);
		}

		[Test]
		public void TwoAbsoluteRowsAndColumns()
		{
			var grid = CreateGridLayout(columns: "100, 100", rows: "10, 30");

			var viewSize = new Size(10, 10);

			var view0 = CreateTestView(viewSize);
			var view1 = CreateTestView(viewSize);
			var view2 = CreateTestView(viewSize);
			var view3 = CreateTestView(viewSize);

			AddChildren(grid, view0, view1, view2, view3);

			SetLocation(grid, view0);
			SetLocation(grid, view1, col: 1);
			SetLocation(grid, view2, row: 1);
			SetLocation(grid, view3, row: 1, col: 1);

			// Assuming no constraints on space
			MeasureAndArrange(grid, double.PositiveInfinity, double.NegativeInfinity);

			// Column width is 100, so despite the view being 10 wide, we expect it to be arranged with width 100
			AssertArranged(view0, 0, 0, 100, 10);

			// Since the first column is 100 wide, we expect the view in the second column to start at x = 100
			AssertArranged(view1, 100, 0, 100, 10);

			// First column, width 100, second row, height 30 and y 10
			AssertArranged(view2, 0, 10, 100, 30);

			// Second column, width 100 and x 100, second row, height 30
			AssertArranged(view3, 100, 10, 100, 30);
		}

		[Test]
		public void TwoAbsoluteColumnsOneAutoRow()
		{
			var grid = CreateGridLayout(columns: "100, 100");

			var viewSize = new Size(10, 10);

			var view0 = CreateTestView(viewSize);
			var view1 = CreateTestView(viewSize);

			AddChildren(grid, view0, view1);

			SetLocation(grid, view0);
			SetLocation(grid, view1, col: 1);

			// Assuming no constraints on space
			MeasureAndArrange(grid, double.PositiveInfinity, double.NegativeInfinity);

			// Column width is 100, so despite the view being 10 wide, we expect it to be arranged with width 100
			AssertArranged(view0, 0, 0, 100, viewSize.Height);

			// Since the first column is 100 wide, we expect the view in the second column to start at x = 100
			AssertArranged(view1, 100, 0, 100, viewSize.Height);
		}

		[Test]
		public void TwoAbsoluteRowsOneAutoColumn()
		{
			var grid = CreateGridLayout(rows: "100, 100");

			var viewSize = new Size(10, 10);

			var view0 = CreateTestView(viewSize);
			var view1 = CreateTestView(viewSize);

			AddChildren(grid, view0, view1);

			SetLocation(grid, view0);
			SetLocation(grid, view1, row: 1);

			// Assuming no constraints on space
			MeasureAndArrange(grid, double.PositiveInfinity, double.NegativeInfinity);

			// Row height is 100, so despite the view being 10 tall, we expect it to be arranged with height 100
			AssertArranged(view0, 0, 0, viewSize.Width, 100);

			// Since the first row is 100 tall, we expect the view in the second row to start at y = 100
			AssertArranged(view1, 0, 100, viewSize.Width, 100);
		}

		[Test(Description = "Row spacing shouldn't affect a single-row grid")]
		public void SingleRowIgnoresRowSpacing()
		{
			var grid = CreateGridLayout(rowSpacing: 10);
			var view = CreateTestView(new Size(100, 100));
			AddChildren(grid, view);
			SetLocation(grid, view);

			MeasureAndArrange(grid, double.PositiveInfinity, double.PositiveInfinity);
			AssertArranged(view, 0, 0, 100, 100);
		}

		[Test(Description = "Two rows should include the row spacing once")]
		public void TwoRowsWithSpacing()
		{
			var grid = CreateGridLayout(rows: "100, 100", rowSpacing: 10);
			var view0 = CreateTestView(new Size(100, 100));
			var view1 = CreateTestView(new Size(100, 100));
			AddChildren(grid, view0, view1);
			SetLocation(grid, view0);
			SetLocation(grid, view1, row: 1);

			MeasureAndArrange(grid, double.PositiveInfinity, double.PositiveInfinity);
			AssertArranged(view0, 0, 0, 100, 100);

			// With column width 100 and spacing of 10, we expect the second column to start at 110
			AssertArranged(view1, 0, 110, 100, 100);
		}

		[Test(Description = "Measure should include row spacing")]
		public void MeasureTwoRowsWithSpacing()
		{
			var grid = CreateGridLayout(rows: "100, 100", rowSpacing: 10);
			var view0 = CreateTestView(new Size(100, 100));
			var view1 = CreateTestView(new Size(100, 100));
			AddChildren(grid, view0, view1);
			SetLocation(grid, view0);
			SetLocation(grid, view1, row: 1);

			var manager = new GridLayoutManager(grid);
			var measure = manager.Measure(double.PositiveInfinity, double.PositiveInfinity);

			Assert.That(measure.Height, Is.EqualTo(100 + 100 + 10));
		}

		[Test(Description = "Column spacing shouldn't affect a single-column grid")]
		public void SingleColumnIgnoresColumnSpacing()
		{
			var grid = CreateGridLayout(colSpacing: 10);
			var view = CreateTestView(new Size(100, 100));
			AddChildren(grid, view);
			SetLocation(grid, view);

			MeasureAndArrange(grid, double.PositiveInfinity, double.PositiveInfinity);
			AssertArranged(view, 0, 0, 100, 100);
		}

		[Test(Description = "Two columns should include the column spacing once")]
		public void TwoColumnsWithSpacing()
		{
			var grid = CreateGridLayout(columns: "100, 100", colSpacing: 10);
			var view0 = CreateTestView(new Size(100, 100));
			var view1 = CreateTestView(new Size(100, 100));
			AddChildren(grid, view0, view1);
			SetLocation(grid, view0);
			SetLocation(grid, view1, col: 1);

			MeasureAndArrange(grid, double.PositiveInfinity, double.PositiveInfinity);
			AssertArranged(view0, 0, 0, 100, 100);

			// With column width 100 and spacing of 10, we expect the second column to start at 110
			AssertArranged(view1, 110, 0, 100, 100);
		}

		[Test(Description = "Measure should include column spacing")]
		public void MeasureTwoColumnsWithSpacing()
		{
			var grid = CreateGridLayout(columns: "100, 100", colSpacing: 10);
			var view0 = CreateTestView(new Size(100, 100));
			var view1 = CreateTestView(new Size(100, 100));
			AddChildren(grid, view0, view1);
			SetLocation(grid, view0);
			SetLocation(grid, view1, col: 1);

			var manager = new GridLayoutManager(grid);
			var measure = manager.Measure(double.PositiveInfinity, double.PositiveInfinity);

			Assert.That(measure.Width, Is.EqualTo(100 + 100 + 10));
		}
	}
}