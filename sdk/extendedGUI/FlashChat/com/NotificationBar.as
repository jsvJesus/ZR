package com
{
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.utils.Timer;
   import flash.utils.setTimeout;
   
   public class NotificationBar extends MovieClip
   {
      public var forMask:Notif_Maska;
      
      public var nothi:MovieClip;
      
      public var background:MovieClip;
      
      public var VISIBLE_TIME:Number = 7000;
      
      public var Text1:TextField;
      
      protected var speed:int = 20;
      
      protected var N:Number = 0;
      
      protected var step:Number = 0;
      
      protected var stepH:Number = 0;
      
      protected var i:int = 0;
      
      protected var tmp_time:Number = 0;
      
      public var timer:Timer = new Timer(this.VISIBLE_TIME);
      
      public function NotificationBar()
      {
         super();
         stage.addEventListener(Event.RESIZE,this.onResizeLog);
         this.visible = false;
         this.addEventListener(MouseEvent.CLICK,this.onMouseClick);
         this.Show({
            "text":"",
            "show_time":1
         });
         this.visible = false;
      }
      
      protected function onMouseClick(e:MouseEvent) : *
      {
         this.Hide();
      }
      
      protected function get_time() : int
      {
         if(this.tmp_time)
         {
            return this.tmp_time;
         }
         return this.VISIBLE_TIME;
      }
      
      protected function onResizeLog(e:Event) : *
      {
         this.x = Object(root).width / 2 - (this.Text1.width + 10) / 2;
      }
      
      public function Show(data:Object) : *
      {
         var line:* = undefined;
         var text:String = data.text;
         this.tmp_time = data.show_time;
         this.Text1.height = 11;
         this.Text1.width = 650;
         this.Text1.htmlText = text;
         var max_width:Number = 0;
         var avg_height:Number = 0;
         var lines:Number = 0;
         var line_list:* = text.split("\n");
         for each(line in line_list)
         {
            this.Text1.htmlText = line;
            if(this.Text1.textWidth + 10 > stage.stageWidth / 2)
            {
               this.Text1.width = stage.stageWidth / 2;
            }
            else
            {
               this.Text1.width = this.Text1.textWidth + 11;
            }
            if(this.Text1.width >= max_width)
            {
               max_width = this.Text1.width;
            }
            avg_height += this.Text1.textHeight;
         }
         for each(line in line_list)
         {
            this.Text1.htmlText = line;
            lines += Math.max(1,Math.round(this.Text1.textWidth / max_width));
         }
         this.Text1.htmlText = text;
         this.Text1.width = max_width;
         avg_height /= lines;
         this.Text1.height = lines * avg_height + 6;
         if(lines == 0)
         {
            this.Text1.height = this.Text1.textHeight + 6;
         }
         this.background.height = this.Text1.height + 12;
         this.background.width = this.Text1.width + 100;
         this.Text1.y = 5;
         this.alpha = 1;
         this.visible = true;
         this.nothi.visible = true;
         this.forMask.x = 0;
         this.forMask.width = 15;
         this.forMask.height = this.Text1.height + 12;
         this.nothi.width = 15;
         this.nothi.x = 0;
         this.N = (this.background.width - this.forMask.width) / this.speed;
         this.step = this.nothi.width / this.N;
         this.i = 0;
         this.timer.reset();
         this.timer.stop();
         this.x = Object(root).width / 2 - (this.background.width + 10) / 2;
         this.removeEventListener(Event.ENTER_FRAME,this.onFrameHide);
         this.addEventListener(Event.ENTER_FRAME,this.onFrame);
      }
      
      protected function Hide() : *
      {
         this.i = 0;
         if(this.timer.currentCount >= 1)
         {
            this.nothi.visible = true;
            this.removeEventListener(Event.ENTER_FRAME,this.onFrame);
            this.addEventListener(Event.ENTER_FRAME,this.onFrameHide);
         }
      }
      
      protected function visibleOff(e:Event) : *
      {
         if(this.alpha > 0)
         {
            this.alpha -= 0.1;
         }
         else
         {
            this.removeEventListener(Event.ENTER_FRAME,this.visibleOff);
            this.visible = false;
         }
      }
      
      protected function visibleOn(e:Event) : *
      {
         if(this.alpha < 1)
         {
            this.alpha += 0.1;
         }
         else
         {
            this.removeEventListener(Event.ENTER_FRAME,this.visibleOn);
            this.visible = true;
         }
      }
      
      protected function onFrameHide(e:Event) : *
      {
         if(this.i < this.N)
         {
            ++this.i;
            this.forMask.width -= this.speed;
            if(this.nothi.width < 33)
            {
               this.nothi.width += this.step;
            }
            this.nothi.x = this.forMask.width + this.forMask.x - this.nothi.width;
         }
         else
         {
            removeEventListener(Event.ENTER_FRAME,this.onFrameHide);
            this.visible = false;
         }
      }
      
      protected function onFrame(e:Event) : *
      {
         if(this.i < this.N)
         {
            ++this.i;
            this.forMask.width += this.speed;
            if(this.nothi.width > 0)
            {
               this.nothi.width -= this.step;
            }
            this.nothi.x = this.forMask.width + this.forMask.x - this.nothi.width;
         }
         else
         {
            this.nothi.visible = false;
            removeEventListener(Event.ENTER_FRAME,this.onFrame);
            this.timer.delay = this.get_time();
            this.timer.start();
            setTimeout(this.Hide,this.get_time());
            this.tmp_time = 0;
         }
      }
   }
}

