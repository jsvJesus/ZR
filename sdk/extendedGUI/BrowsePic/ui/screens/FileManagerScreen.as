package ui.screens
{
   import ui.Navigator;
   import ui.Screen;
   import ui.components.FileManagerWindow;
   
   public class FileManagerScreen extends Screen
   {
      private var fileManagerWindow:FileManagerWindow;
      
      public function FileManagerScreen(id:String, depth:uint = 0)
      {
         super(id,depth);
      }
      
      override protected function unfreeze(... args) : void
      {
      }
      
      override protected function resize(... args) : void
      {
      }
      
      override protected function init(... args) : void
      {
         this.fileManagerWindow = new FileManagerWindow(this);
         this.fileManagerWindow.draggable = true;
         this.resize();
         this.fileManagerWindow.setSize(660,533);
         this.fileManagerWindow.x = (Base.stage.stageWidth - this.fileManagerWindow.width) / 2;
         this.fileManagerWindow.y = (Navigator.ScreenHeight - this.fileManagerWindow.height) / 2;
      }
   }
}

