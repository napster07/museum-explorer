import {defineArrayMember, defineField, defineType} from 'sanity'

export const exhibition = defineType({
  name: 'exhibition',
  title: 'Exhibition',
  type: 'document',
  fields: [
    defineField({
      name: 'title', type: 'string',
      validation: rule => rule.required().max(120),
    }),
    defineField({
      name: 'slug', type: 'slug', options: {source: 'title', maxLength: 100},
      description: 'A unique address for this exhibition. Generate it from the title.',
      validation: rule => rule.required(),
    }),
    defineField({
      name: 'category', type: 'string',
      options: {list: ['Natural world', 'Art & design', 'Science']},
      validation: rule => rule.required().custom(value => !value || ['Natural world', 'Art & design', 'Science'].includes(value) || 'Choose a listed category.'),
    }),
    defineField({
      name: 'summary', type: 'text', rows: 3,
      validation: rule => rule.required().max(240),
    }),
    defineField({
      name: 'description', type: 'text', rows: 6,
      description: 'Plain text shown when a visitor expands the exhibition details.',
      validation: rule => rule.required().max(4000),
    }),
    defineField({
      name: 'gallery', type: 'string',
      validation: rule => rule.required().max(80),
    }),
    defineField({
      name: 'minutes', title: 'Estimated visit duration (minutes)', type: 'number',
      validation: rule => rule.required().integer().min(1).max(240),
    }),
    defineField({
      name: 'colour', title: 'Illustration theme', type: 'string',
      options: {list: [{title: 'Ocean', value: 'ocean'}, {title: 'Light', value: 'light'}, {title: 'Earth', value: 'earth'}]},
      initialValue: 'ocean',
      validation: rule => rule.required().custom(value => !value || ['ocean', 'light', 'earth'].includes(value) || 'Choose a listed theme.'),
    }),
    defineField({
      name: 'access', title: 'Accessibility features', type: 'array',
      of: [defineArrayMember({type: 'string'})],
      description: 'List only features verified for the actual exhibition.',
      validation: rule => rule.unique(),
    }),
  ],
  preview: {select: {title: 'title', subtitle: 'category'}},
})
