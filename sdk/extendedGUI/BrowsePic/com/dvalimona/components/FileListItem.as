package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.MouseEvent;
   
   public class FileListItem extends ListItem
   {
      public function FileListItem(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, data:Object = null)
      {
         _data = data;
         super(parent,xpos,ypos);
      }
      
      override protected function init() : void
      {
         super.init();
         super.addEventListener(MouseEvent.MOUSE_OVER,onMouseOver);
         setSize(100,20);
      }
      
      protected function onDoubleClick(event:MouseEvent) : void
      {
         this.alpha = 0;
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         _label = new Label(this,20,3);
         _label.color = 0;
         _label.size = 20;
         _label.draw();
      }
      
      override public function draw() : void
      {
         var dataText:String = null;
         super.draw();
         graphics.clear();
         if(_selected)
         {
            graphics.beginFill(_selectedColor);
         }
         else if(_mouseOver)
         {
            graphics.beginFill(_rolloverColor);
         }
         else
         {
            graphics.beginFill(_defaultColor,1);
         }
         graphics.drawRect(0,0,width,height);
         graphics.endFill();
         if(_data == null)
         {
            return;
         }
         dataText = _data.label;
         dataText = dataText.length > 50 ? dataText.substr(0,70) + "..." : dataText;
         if(Boolean(_data.isDir))
         {
            _label.htmlText = "<font color=\'#444444\' size=\'18\' face=\'" + Base.FONT_BOLD + "\'>" + "[" + "</font>" + "<font color=\'#444444\' size=\'18\'  face=\'" + Base.FONT_BOLD + "\'>" + "" + dataText + "" + "</font>" + "<font color=\'#444444\' size=\'18\'  face=\'" + Base.FONT_BOLD + "\'>" + "]" + "</font>";
         }
         else
         {
            _label.htmlText = "<font color=\'#000000\' size=\'18\'  face=\'" + Base.FONT_LIGHT + "\'>" + dataText + "</font>";
         }
      }
   }
}

