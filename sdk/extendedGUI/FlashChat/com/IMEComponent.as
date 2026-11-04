package com
{
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFormat;
   
   public class IMEComponent extends MovieClip
   {
      public var background:Sprite;
      
      public var components:Array = new Array();
      
      public function IMEComponent()
      {
         super();
         this.visible = false;
         this.height = 20;
         this.initComponents();
      }
      
      protected function initComponents() : *
      {
         var text:TextField = null;
         var textformat:TextFormat = new TextFormat();
         var font:Arial_MS = new Arial_MS();
         textformat.font = font.fontName;
         textformat.size = 16;
         textformat.color = 16777215;
         this.background = new Sprite();
         this.addChild(this.background);
         for(var i:int = 0; i < 9; i++)
         {
            text = new TextField();
            text.autoSize = "true";
            text.embedFonts = true;
            text.defaultTextFormat = textformat;
            this.addChild(text);
            text.x = 0;
            text.y = 0;
            this.components.push(text);
         }
         this.setChildIndex(this.background,0);
      }
      
      protected function onMouseClick(e:MouseEvent) : *
      {
         this.Hide();
      }
      
      public function updatePosition(x:int, y:int) : *
      {
         this.x = x;
         this.y = y;
      }
      
      public function showCandidates(data:Array) : *
      {
         var text:* = undefined;
         var index_string:* = undefined;
         var index:int = 0;
         var x:int = 0;
         for(var i:int = 0; i < 9; i++)
         {
            this.components[i].text = "";
         }
         for each(text in data)
         {
            index_string = (index + 1).toString();
            this.components[index].text = index_string + " " + text;
            this.components[index].x = x;
            x = x + this.components[index].textWidth + 4;
            index++;
         }
         this.background.graphics.clear();
         this.background.graphics.beginFill(0,0.9);
         this.background.graphics.drawRect(0,0,x,40);
         this.background.graphics.endFill();
         this.visible = true;
      }
      
      protected function Hide() : *
      {
      }
   }
}

