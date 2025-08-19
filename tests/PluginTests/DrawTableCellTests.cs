namespace PluginTests;

/// <summary>
/// TableRenderSource.RenderTableCells()関連のテスト
/// </summary>
public class DrawTableCellTests
{
	[Fact]
	public void Test1() { }

	//仕様に沿って描画しているかどうかをテスト
	//仕様：
	//・A:テーブルの一番外枠(OuterBorder)はテーブルの描画領域いっぱい描画し、はみ出ない
	//・B:テーブルの枠とセルの間のグリッド線はBorderで同じ枠線として扱われる
	//・C:セルの内枠は外枠OuterBorderと同じ幅・色で描画される
	//・それぞれの枠線は重ならない（ただしグリッド線を交差して描画する場合を除く）
	//・セルの背景枠は枠線との間に隙間なく描画される
	//・セルの内部の文字は、枠線とかぶることなく描画される
	//・A/B/Cそれぞれの枠線は隣接する場合は隙間はできない
	//・分割数に合わせてセルの数は増えるが、それぞれのセルの大きさは同一
	//・セルの大きさは同一なので、特にセルの内枠も同じ大きさ・領域
}
