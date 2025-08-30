using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using YMM4TableShapePlugin.ViewModels;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Views.Converters;

namespace YMM4TableShapePlugin.View;

[AttributeUsage(AttributeTargets.Property)]
internal class TableShapeEditorAttribute
	: PropertyEditorAttribute2
{
	/// <summary>
	/// コントロールを作成する
	/// ここで返すコントロールはIPropertyEditorControlを実装している必要がある
	/// </summary>
	/// <returns></returns>
	public override FrameworkElement Create()
	{
		return new TableShapeEditor();
	}

	/// <summary>
	/// コントロールにバインディングを設定する（複数編集対応版）
	/// </summary>
	/// <param name="control">Create()で作成したコントロール</param>
	/// <param name="itemProperties">編集対象のプロパティ</param>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"SMA0040"
	)]
	public override void SetBindings(
		FrameworkElement control,
		ItemProperty[] itemProperties
	)
	{
		if (control is not TableShapeEditor editor)
		{
			return;
		}

		editor.DataContext = new TableShapeEditorViewModel(
			itemProperties
		);

		/*
		editor.SetBinding(
			TableShapeView.ValueProperty,
			ItemPropertiesBinding.Create(itemProperties)
		);
		*/
	}

	/// <summary>
	/// バインディングを解除する
	/// </summary>
	/// <param name="control"></param>
	public override void ClearBindings(
		FrameworkElement control
	)
	{
		if (control is not TableShapeEditor editor)
		{
			return;
		}

		if (editor.DataContext is TableShapeEditorViewModel vm)
		{
			vm.Dispose();
		}
		editor.DataContext = null;

		/*
		BindingOperations.ClearBinding(
			control,
			TableShapeView.ValueProperty
		);
		*/
	}
}
