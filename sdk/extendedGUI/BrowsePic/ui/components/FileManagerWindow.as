package ui.components
{
   import com.dvalimona.components.ClearButton;
   import com.dvalimona.components.FileListItem;
   import com.dvalimona.components.HBox;
   import com.dvalimona.components.Label;
   import com.dvalimona.components.List;
   import com.dvalimona.components.PushButton;
   import com.dvalimona.components.VBox;
   import com.dvalimona.components.Window;
   import communication.Api;
   import communication.FileItem;
   import communication.FileManager;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.utils.Dictionary;
   import logging.Logger;
   
   public class FileManagerWindow extends Window
   {
      protected var vBox:VBox;
      
      protected var topLine:HBoxLine;
      
      protected var rootBox:HBox;
      
      protected var path:Label;
      
      protected var upLevel:ClearButton;
      
      protected var list:List;
      
      protected var buttonsBox:HBox;
      
      protected var buttonsBoxHeight:uint = 60;
      
      protected var okButton:PushButton;
      
      protected var cancelButton:PushButton;
      
      protected var root_helper:Dictionary;
      
      public function FileManagerWindow(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, title:String = "Window")
      {
         FileManager.self.addEventListener(FileManager.ROOTS,this.onRootsHandler);
         FileManager.self.addEventListener(FileManager.LIST,this.onListHandler);
         super(parent,xpos,ypos,title);
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
      }
      
      override protected function onMouseGoDown(event:MouseEvent) : void
      {
         trace("??onMouseGoDown",_draggable,header.contains(event.target as DisplayObject));
         if(_draggable && header.contains(event.target as DisplayObject))
         {
            this.startDrag();
            stage.addEventListener(MouseEvent.MOUSE_UP,onMouseGoUp);
         }
         dispatchEvent(new Event(Event.SELECT));
      }
      
      private function headerDeals() : void
      {
         leftItems.shift = 3;
         _titleLabel.font = Base.FONT_BOLD;
         _titleLabel.text = "ВЫБОР ФАЙЛА";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
      }
      
      private function bodyDeals() : void
      {
         this.vBox = new VBox(this);
         this.vBox.alignment = VBox.LEFT;
         this.vBox.spacing = 1;
         super.addChild(this.vBox);
         this.topLine = new HBoxLine(this.vBox);
         this.topLine.height = 30;
         this.path = new Label(this.topLine.left);
         this.path.font = Base.FONT_LIGHT;
         this.path.size = 20;
         this.path.text = "path will be here";
         this.path.debug = false;
         this.path.padding = 20;
         this.path.autoSize = true;
         this.list = new List(this.vBox);
         this.list.listItemClass = FileListItem;
         this.list.autoHideScrollBar = true;
         this.list.listItemHeight = 30;
         this.list.addEventListener(Event.OPEN,this.onOpenHandler);
         this.list.addEventListener(Event.SELECT,this.onSelectHandler);
         this.buttonsBox = new HBox();
         this.buttonsBox.alignment = HBox.MIDDLE;
         this.buttonsBox.fixedHeight = this.buttonsBoxHeight;
         this.buttonsBox.fixedWidth = 460;
         this.buttonsBox.horizontalAlign = HBox.LEFT;
         this.buttonsBox.spacing = 1;
         this.buttonsBox.debug = false;
         super.addChild(this.buttonsBox);
         this.okButton = new PushButton();
         this.okButton.addEventListener(MouseEvent.CLICK,this.onOkClickHandler);
         this.okButton.font = Base.FONT_LIGHT;
         this.okButton.size = 22;
         this.okButton.label = "OK";
         this.okButton.labelUpColor = 5626367;
         this.okButton.labelOverColor = 10347511;
         this.okButton.overColorAlpha = 1;
         this.buttonsBox.addChild(this.okButton);
         this.cancelButton = new PushButton();
         this.cancelButton.addEventListener(MouseEvent.CLICK,this.onCancelClickHandler);
         this.cancelButton.font = Base.FONT_BOLD;
         this.cancelButton.size = 22;
         this.cancelButton.label = "Cancel";
         this.cancelButton.overColorAlpha = 1;
         this.buttonsBox.addChild(this.cancelButton);
         this.fillRootsPanel();
         this.fillList();
      }
      
      protected function onOpenHandler(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onOpenHandler",this.list.selectedItem.label,this.list.selectedItem.isDir,this.list.selectedItem.isUpLevel,this.list.selectedItem.isRoot,FileManager.isRoot);
         this.okButton.enabled = false;
         var selectedItem:FileItem = this.list.selectedItem as FileItem;
         if(Boolean(this.list.selectedItem.isDir))
         {
            if(selectedItem.isUpLevel)
            {
               this.path.htmlText = "";
               this.list.removeAll();
               if(FileManager.isRoot)
               {
                  this.pushRootsToFileList();
               }
               else
               {
                  Api.call(Api.UP_LEVEL);
               }
            }
            else
            {
               this.path.htmlText = "";
               this.list.removeAll();
               if(selectedItem.isRoot)
               {
                  Api.call(Api.CHOOSE_DIRECTORY,[{"filename":selectedItem.label}]);
               }
               else
               {
                  Api.call(Api.CHOOSE_DIRECTORY,[{"filename":selectedItem.label}]);
               }
            }
         }
         else
         {
            this.doOk();
         }
      }
      
      protected function onSelectHandler(event:Event) : void
      {
         this.okButton.enabled = !this.list.selectedItem.isDir;
      }
      
      protected function onCancelClickHandler(event:MouseEvent) : void
      {
         this.doCancel();
      }
      
      protected function onOkClickHandler(event:MouseEvent) : void
      {
         this.doOk();
      }
      
      private function doOk() : void
      {
         Api.call(Api.CHOOSE_FILE,[{"filename":this.list.selectedItem.label}]);
      }
      
      private function doCancel() : void
      {
         Api.call(Api.PUSH_CANCEL);
      }
      
      protected function onListHandler(event:Event) : void
      {
         this.fillList();
      }
      
      protected function onRootsHandler(event:Event) : void
      {
         this.fillRootsPanel();
      }
      
      protected function pushRootsToFileList() : void
      {
         var roots:Array = FileManager.RootList.concat([]);
         for(var i:uint = 0; i < roots.length; i++)
         {
            this.list.addItem(new FileItem({
               "label":roots[i],
               "isDir":true,
               "isUpLevel":false,
               "isRoot":true
            }));
         }
      }
      
      protected function fillRootsPanel() : void
      {
         var rootButton:PushButton = null;
         this.root_helper = new Dictionary();
         var roots:Array = FileManager.RootList.concat([]);
         roots.reverse();
         for(var i:uint = 0; i < roots.length; i++)
         {
            rootButton = new PushButton(this.topLine.right);
            rootButton.size = 18;
            rootButton.width = 40;
            rootButton.height = this.topLine.height;
            rootButton.label = roots[i];
            rootButton.addEventListener(MouseEvent.CLICK,this.onRootClick);
            this.root_helper[rootButton] = roots[i];
         }
      }
      
      protected function onRootClick(event:MouseEvent) : void
      {
         this.path.htmlText = "";
         this.list.removeAll();
         Api.call(Api.CHANGE_ROOT,[{"root":String(this.root_helper[event.target])}]);
      }
      
      protected function fillList() : void
      {
         var obj:Object = null;
         var absPath:String = FileManager.absPath;
         absPath = absPath.length > 50 ? "..." + absPath.substring(absPath.length - 50,absPath.length) : absPath;
         this.path.htmlText = "<font color=\'#FFFFFF\' size=\'20\' face=\'" + Base.FONT_BOLD + "\'>" + "[" + "</font>" + "<font color=\'#55d9ff\' size=\'20\'  face=\'" + Base.FONT_LIGHT + "\'>" + " " + absPath + " " + "</font>" + "<font color=\'#FFFFFF\' size=\'20\'  face=\'" + Base.FONT_BOLD + "\'>" + "]" + "</font>";
         this.list.removeAll();
         var allItems:Array = [];
         this.list.addItem(new FileItem({
            "label":"..",
            "isDir":true,
            "isUpLevel":true,
            "isRoot":false
         }));
         for(var i:uint = 0; i < FileManager.FileList.length; i++)
         {
            obj = FileManager.FileList[i];
            this.list.addItem(new FileItem({
               "label":obj.name,
               "isDir":obj.is_dir == 1,
               "isUpLevel":false,
               "isRoot":false
            }));
         }
      }
      
      override public function draw() : void
      {
         super.draw();
         this.topLine.width = _width;
         this.vBox.x = 0;
         this.vBox.y = 0;
         this.vBox.width = _width;
         this.vBox.height = _height - headerHeight - this.buttonsBoxHeight;
         this.list.width = _width;
         this.list.height = this.vBox.height - this.list.y - 2;
         this.okButton.setSize(width * 0.6 - 1,this.buttonsBoxHeight);
         this.cancelButton.setSize(width * 0.4,this.buttonsBoxHeight);
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

