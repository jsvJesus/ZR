package com.dvalimona.components
{
   import flash.display.*;
   import flash.events.*;
   import logging.*;
   import ui.components.*;
   
   public class PremiumList extends List
   {
      public function PremiumList(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, items:Array = null)
      {
         _listItemClass = PremiumListItem;
         super(parent,xpos,ypos,items);
      }
      
      override protected function addChildren() : void
      {
         _panel = new Panel(this,0,0);
         _panel.color = _defaultColor;
         _panel.colorAlpha = 0;
         _itemHolder = new Sprite();
         _panel.content.addChild(_itemHolder);
         _scrollbar = new VScrollBar(this,0,0,onScroll);
         _scrollbar.hideButtons = true;
         _scrollbar.autoHide = true;
         _scrollbar.setSliderParams(0,0,0);
      }
      
      override protected function makeListItems() : void
      {
         var item:PremiumListItem = null;
         var numItems:int = 0;
         var i:int = 0;
         try
         {
            while(_itemHolder.numChildren > 0)
            {
               item = PremiumListItem(_itemHolder.getChildAt(0));
               item.removeEventListener(MouseEvent.CLICK,onSelect);
               _itemHolder.removeChildAt(0);
            }
            _listItems = new Array();
            numItems = Math.ceil(_height / _listItemHeight);
            numItems = Math.min(numItems,_items.length);
            numItems = Math.max(numItems,0);
            for(i = 0; i < numItems; i++)
            {
               item = new PremiumListItem(_itemHolder,0,i * _listItemHeight + i * spacing);
               _listItems.push(item);
               item.setSize(width - _scrollbar.width,_listItemHeight);
               item.defaultColor = _defaultColor;
               item.selectedColor = _selectedColor;
               item.rolloverColor = _rolloverColor;
               item.addEventListener(MouseEvent.CLICK,onSelect);
               item.doubleClickEnabled = true;
               item.addEventListener(MouseEvent.DOUBLE_CLICK,onDoubleClick);
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.DEBUG,"ERROR PremiumList.makeListItems:",error);
         }
      }
      
      override protected function fillItems() : void
      {
         var offset:int = 0;
         var numItems:int = 0;
         var i:int = 0;
         var item:PremiumListItem = null;
         try
         {
            offset = _scrollbar.value;
            numItems = Math.ceil(_height / _listItemHeight);
            numItems = Math.min(numItems,_items.length);
            Logger.LogToChannel(Logger.DEBUG,"PremiumList.fillItems:numItems",numItems);
            for(i = 0; i < numItems; i++)
            {
               item = _listItems[i];
               if(offset + i < _items.length)
               {
                  item.data = _items[offset + i];
               }
               else
               {
                  item.data = null;
               }
               if(offset + i == _selectedIndex)
               {
                  item.selected = true;
               }
               else
               {
                  item.selected = false;
               }
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.DEBUG,"ERROR PremiumList.fillItems:",error);
         }
      }
   }
}

