using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YMM4TableShapePlugin.ViewModels;
using YukkuriMovieMaker.Commons;

namespace YMM4TableShapePlugin.View;

/// <summary>
/// IncreaseDecreaseButton.xaml の相互作用ロジック
/// IPropertyEditorControlを実装する必要がある
/// </summary>
public partial class TableShapeEditor
	: UserControl,
		IPropertyEditorControl2
{
	IEditorInfo? EditorInfo { get; set; }

	public event EventHandler? BeginEdit;
	public event EventHandler? EndEdit;

	public YukkuriMovieMaker.Controls.PropertiesEditor PropertyEditorControl =>
		innerPropertyEditor;

	public TableShapeEditor()
	{
		InitializeComponent();
		DataContextChanged +=
			InnerPropertyEditor_DataContextChanged;
	}

	public void SetEditorInfo(IEditorInfo info)
	{
		EditorInfo = info;
		if(info is null){ return; }

		if (DataContext is TableShapeEditorViewModel vm)
		{
			vm.EditorInfo = info;
		}
		innerPropertyEditor.SetEditorInfo(info);
	}

	void InnerPropertyEditor_DataContextChanged(
		object sender,
		DependencyPropertyChangedEventArgs e
	)
	{
		if (e.OldValue is TableShapeEditorViewModel oldVm)
		{
			oldVm.BeginEdit -=
				InnerPropertiesEditor_BeginEdit;
			oldVm.EndEdit -= InnerPropertiesEditor_EndEdit;
		}
		if (e.NewValue is TableShapeEditorViewModel newVm)
		{
			newVm.BeginEdit +=
				InnerPropertiesEditor_BeginEdit;
			newVm.EndEdit += InnerPropertiesEditor_EndEdit;
			newVm.EditorInfo = EditorInfo;
		}
	}

	/// <summary>
	/// 内蔵プロパティエディタ用BeginEdit
	/// </summary>
	void InnerPropertiesEditor_BeginEdit(
		object? sender,
		EventArgs e
	)
	{
		BeginEdit?.Invoke(this, e);
	}

	/// <summary>
	/// 内蔵プロパティエディタ用EndEdit
	/// </summary>
	void InnerPropertiesEditor_EndEdit(
		object? sender,
		EventArgs e
	)
	{
		if (DataContext is TableShapeEditorViewModel vm) {
			vm.ForceRefresh();
		}
		EndEdit?.Invoke(this, e);
	}
}